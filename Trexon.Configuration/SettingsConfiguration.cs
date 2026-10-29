using System.Collections;
using Trexon.Configuration.Internal;

namespace Trexon.Configuration;

/// <summary>Configures ordered sources, highest-priority overrides and manager registrations.</summary>
public sealed class SettingsConfiguration
{
    internal readonly List<Func<IReadOnlyDictionary<string, RawValue>>> Sources = [];
    internal readonly Dictionary<string, string> Overrides = new(StringComparer.Ordinal);
    internal readonly List<Type> Registrations = [];

    /// <summary>Creates a builder. Environment variables are the default source when none is added.</summary>
    public SettingsConfiguration() => ReadFrom = new SettingsSourceConfiguration(this);

    /// <summary>Adds sources in increasing priority order: the last source wins.</summary>
    public SettingsSourceConfiguration ReadFrom { get; }

    /// <summary>Adds an exact key override. Overrides always win over every source.</summary>
    public SettingsConfiguration Override(string key, string value)
    {
        KeyNames.Validate(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value);
        Overrides[key] = value;
        return this;
    }

    /// <summary>Registers a settings class for eager loading by a manager. Duplicate registrations fail.</summary>
    public SettingsConfiguration Register<T>() where T : class
    {
        if (Registrations.Contains(typeof(T)))
            throw new ArgumentException($"Settings type {typeof(T).Name} is already registered.");
        Registrations.Add(typeof(T));
        return this;
    }

    internal Dictionary<string, RawValue> ReadValues()
    {
        var values = new Dictionary<string, RawValue>(StringComparer.Ordinal);
        var sources = Sources.ToArray();
        if (sources.Length == 0) sources = [SettingsSourceConfiguration.ReadEnvironment];
        foreach (var read in sources)
            foreach (var pair in read()) values[pair.Key] = pair.Value;
        foreach (var pair in Overrides) values[pair.Key] = new(pair.Value, "override");
        return values;
    }
}

/// <summary>Built-in sources. Reading never modifies the process environment.</summary>
public sealed class SettingsSourceConfiguration
{
    private readonly SettingsConfiguration _owner;
    internal SettingsSourceConfiguration(SettingsConfiguration owner) => _owner = owner;

    /// <summary>Reads a snapshot of the current process environment at load or manager creation time.</summary>
    public SettingsConfiguration EnvironmentVariables()
    {
        _owner.Sources.Add(ReadEnvironment);
        return _owner;
    }

    /// <summary>Reads a UTF-8 dotenv file. Only a missing file is ignored when optional is true.</summary>
    public SettingsConfiguration DotEnv(string path, bool optional = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        _owner.Sources.Add(() => DotEnvReader.Read(fullPath, optional));
        return _owner;
    }

    /// <summary>Adds a copied dictionary source, useful for isolated tests. Keys are case-sensitive.</summary>
    public SettingsConfiguration InMemory(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var snapshot = new Dictionary<string, RawValue>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            KeyNames.Validate(pair.Key, nameof(values));
            ArgumentNullException.ThrowIfNull(pair.Value);
            snapshot.Add(pair.Key, new(pair.Value, "memory"));
        }
        _owner.Sources.Add(() => snapshot);
        return _owner;
    }

    internal static IReadOnlyDictionary<string, RawValue> ReadEnvironment()
    {
        try
        {
            var result = new Dictionary<string, RawValue>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                if (entry.Key is string key && entry.Value is string value)
                    result[key] = new(value, "environment");
            return result;
        }
        catch (Exception)
        {
            throw new SettingsException([new("$source", null, "environment", SettingsErrorCode.Source,
                "Cannot read process environment.")]);
        }
    }
}
