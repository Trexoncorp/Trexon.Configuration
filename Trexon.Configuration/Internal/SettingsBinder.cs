using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Trexon.Configuration.Internal;

internal static class SettingsBinder
{
    private sealed record Member(PropertyInfo Property, string Path, string? Key, bool Secret,
        ValidationAttribute[] Validators, Model? Nested);
    private sealed record Model(Type Type, string Path, Member[] Members, ValidationAttribute[] Validators);

    internal static object Load(Type type, IReadOnlyDictionary<string, RawValue> values)
    {
        var errors = new List<SettingsError>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var model = Build(type, type.Name, "", false, [], keys, errors);
        if (errors.Count != 0) throw new SettingsException(errors);
        var instance = Bind(model!, null, values, errors);
        if (errors.Count != 0) throw new SettingsException(errors);
        return instance!;
    }

    private static Model? Build(Type type, string path, string prefix, bool secret, HashSet<Type> ancestors,
        HashSet<string> keys, List<SettingsError> errors)
    {
        if (!type.IsClass || type.IsAbstract || type == typeof(object) || typeof(IEnumerable).IsAssignableFrom(type)
            || type.GetConstructor(Type.EmptyTypes) == null)
        {
            errors.Add(Error(path, null, "model", SettingsErrorCode.Model,
                "Expected a settings class with a public parameterless constructor.", secret));
            return null;
        }
        if (ancestors.Contains(type) || ancestors.Count >= 32)
        {
            errors.Add(Error(path, null, "model", SettingsErrorCode.Model, "Recursive or excessively deep settings model.", secret));
            return null;
        }
        var branch = new HashSet<Type>(ancestors) { type };
        var members = new List<Member>();
        try
        {
            prefix += type.GetCustomAttribute<EnvPrefixAttribute>(false)?.Prefix ?? "";
            foreach (var group in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.GetIndexParameters().Length == 0).GroupBy(p => p.Name).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var memberPath = path + "." + group.Key;
                if (group.Count() != 1)
                {
                    errors.Add(Error(memberPath, null, "model", SettingsErrorCode.Model, "Hidden properties with the same name are unsupported.", secret));
                    continue;
                }
                var property = group.Single();
                var alias = property.GetCustomAttribute<EnvAttribute>(true);
                var markedSecret = property.IsDefined(typeof(SecretAttribute), true);
                var sensitive = secret || markedSecret;
                var validators = property.GetCustomAttributes<ValidationAttribute>(true).ToArray();
                if (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
                {
                    if (alias != null || markedSecret || validators.Length != 0)
                        errors.Add(Error(memberPath, null, "model", SettingsErrorCode.Model,
                            "Annotated settings require public get and set/init accessors.", sensitive));
                    continue; // Unannotated computed and read-only properties are not settings.
                }
                Model? nested = null;
                string? key = null;
                if (ValueConversion.IsScalar(property.PropertyType))
                {
                    key = alias?.Name ?? prefix + KeyNames.FromProperty(property.Name);
                    if (!KeyNames.IsValid(key))
                        errors.Add(Error(memberPath, null, "model", SettingsErrorCode.Model, "Generated key is not a valid ASCII environment key.", sensitive));
                    else if (!keys.Add(key))
                        errors.Add(Error(memberPath, key, "model", SettingsErrorCode.Model, "Multiple properties map to the same environment key.", sensitive));
                }
                else
                {
                    if (alias != null)
                        errors.Add(Error(memberPath, alias.Name, "model", SettingsErrorCode.Model, "Env aliases apply only to scalar properties.", sensitive));
                    nested = Build(property.PropertyType, memberPath, prefix + KeyNames.FromProperty(property.Name) + "__",
                        sensitive, branch, keys, errors);
                }
                members.Add(new(property, memberPath, key, sensitive, validators, nested));
            }
            return new(type, path, members.ToArray(), type.GetCustomAttributes<ValidationAttribute>(true).ToArray());
        }
        catch (Exception)
        {
            errors.Add(Error(path, null, "model", SettingsErrorCode.Model, "Cannot inspect settings model attributes.", secret));
            return null;
        }
    }

    private static object? Bind(Model model, object? instance, IReadOnlyDictionary<string, RawValue> values,
        List<SettingsError> errors)
    {
        try { instance ??= Activator.CreateInstance(model.Type)!; }
        catch (Exception)
        {
            errors.Add(Error(model.Path, null, "model", SettingsErrorCode.Model, "Settings constructor failed."));
            return null;
        }

        var initialErrorCount = errors.Count;
        var validations = new List<(Member Member, string Source)>();
        foreach (var member in model.Members)
        {
            var source = "default";
            object? value;
            try { value = member.Property.GetValue(instance); }
            catch (Exception)
            {
                errors.Add(Error(member.Path, member.Key, source, SettingsErrorCode.Model, "Settings getter failed.", member.Secret));
                continue;
            }
            if (member.Nested != null)
            {
                // A null nested group stays null unless one of its known keys is supplied.
                if (value != null || HasValues(member.Nested, values))
                {
                    value = Bind(member.Nested, value, values, errors);
                    if (!Set(instance, member, value, source, errors)) continue;
                }
            }
            else if (values.TryGetValue(member.Key!, out var raw))
            {
                source = raw.Source;
                try { value = ValueConversion.Convert(raw.Text, member.Property.PropertyType); }
                catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
                {
                    var typeName = Nullable.GetUnderlyingType(member.Property.PropertyType)?.Name ?? member.Property.PropertyType.Name;
                    errors.Add(Error(member.Path, member.Key, source, SettingsErrorCode.Conversion,
                        $"Cannot convert supplied value to {typeName}.", member.Secret));
                    continue;
                }
                if (!Set(instance, member, value, source, errors)) continue;
            }
            validations.Add((member, source));
        }
        // Rules such as CompareAttribute must see the final values of all sibling properties.
        foreach (var (member, source) in validations)
        {
            try
            {
                var value = member.Property.GetValue(instance);
                Validate(member.Validators, value, instance, member.Path, member.Property.Name, member.Key, source, member.Secret, errors);
            }
            catch (Exception)
            {
                errors.Add(Error(member.Path, member.Key, source, SettingsErrorCode.Model, "Settings getter failed.", member.Secret));
            }
        }
        // Object rules run only after property binding and validation have succeeded.
        if (errors.Count == initialErrorCount)
        {
            Validate(model.Validators, instance, instance, model.Path, null, null, "model", false, errors);
            if (instance is IValidatableObject validatable)
            {
                try
                {
                    foreach (var result in validatable.Validate(new ValidationContext(instance)))
                        if (result != ValidationResult.Success)
                            errors.Add(Error(model.Path, null, "model", SettingsErrorCode.Validation, "Object validation failed (IValidatableObject)."));
                }
                catch (Exception)
                {
                    errors.Add(Error(model.Path, null, "model", SettingsErrorCode.Validation, "Object validator failed."));
                }
            }
        }
        return instance;
    }

    private static bool Set(object instance, Member member, object? value, string source, List<SettingsError> errors)
    {
        try { member.Property.SetValue(instance, value); return true; }
        catch (Exception)
        {
            errors.Add(Error(member.Path, member.Key, source, SettingsErrorCode.Model, "Settings setter failed.", member.Secret));
            return false;
        }
    }

    private static bool HasValues(Model model, IReadOnlyDictionary<string, RawValue> values)
        => model.Members.Any(m => m.Key != null ? values.ContainsKey(m.Key) : m.Nested != null && HasValues(m.Nested, values));

    private static void Validate(ValidationAttribute[] validators, object? value, object instance, string path,
        string? memberName, string? key, string source, bool secret, List<SettingsError> errors)
    {
        foreach (var validator in validators)
        {
            try
            {
                var context = new ValidationContext(instance) { MemberName = memberName };
                if (validator.GetValidationResult(value, context) != ValidationResult.Success)
                    errors.Add(Error(path, key, source, SettingsErrorCode.Validation, $"Validation failed ({validator.GetType().Name}).", secret));
            }
            catch (Exception)
            {
                errors.Add(Error(path, key, source, SettingsErrorCode.Validation, $"Validator failed ({validator.GetType().Name}).", secret));
            }
        }
    }

    private static SettingsError Error(string path, string? key, string source, SettingsErrorCode code, string message, bool secret = false)
        => new(path, key, source, code, message, secret);
}
