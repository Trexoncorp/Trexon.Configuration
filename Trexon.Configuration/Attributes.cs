namespace Trexon.Configuration;

/// <summary>Uses an exact environment key instead of the generated name, bypassing all prefixes.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnvAttribute : Attribute
{
    /// <summary>Creates an exact environment key mapping.</summary>
    public EnvAttribute(string name)
    {
        Internal.KeyNames.Validate(name, nameof(name));
        Name = name;
    }

    /// <summary>The exact, case-sensitive environment key.</summary>
    public string Name { get; }
}

/// <summary>Adds a literal prefix to generated keys for this class. Base class prefixes are not inherited.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EnvPrefixAttribute : Attribute
{
    /// <summary>Creates a prefix such as APP_ or VAPID_. An empty prefix is allowed.</summary>
    public EnvPrefixAttribute(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (prefix.Length != 0) Internal.KeyNames.Validate(prefix, nameof(prefix));
        Prefix = prefix;
    }

    /// <summary>The literal prefix, including any desired trailing underscore.</summary>
    public string Prefix { get; }
}

/// <summary>Marks a scalar setting as sensitive in error metadata. Does not change its stored value.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SecretAttribute : Attribute;
