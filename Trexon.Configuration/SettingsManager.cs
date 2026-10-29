using Trexon.Configuration.Internal;

namespace Trexon.Configuration;

/// <summary>An eagerly validated collection of settings objects loaded from one shared source snapshot.</summary>
public sealed class SettingsManager
{
    private readonly Dictionary<Type, object> _settings;
    private SettingsManager(Dictionary<Type, object> settings) => _settings = settings;

    /// <summary>Reads sources once and loads every registered type. Any failure prevents manager creation.</summary>
    public static SettingsManager Create(Action<SettingsConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configuration = new SettingsConfiguration();
        configure(configuration);
        var types = configuration.Registrations.ToArray();
        if (types.Length == 0) throw new InvalidOperationException("Register at least one settings type.");
        var values = configuration.ReadValues();
        var loaded = new Dictionary<Type, object>();
        var errors = new List<SettingsError>();
        foreach (var type in types)
        {
            try { loaded.Add(type, SettingsBinder.Load(type, values)); }
            catch (SettingsException exception) { errors.AddRange(exception.Errors); }
        }
        if (errors.Count != 0) throw new SettingsException(errors);
        return new SettingsManager(loaded);
    }

    /// <summary>Returns the same loaded instance on each call. An unregistered type throws.</summary>
    public T Get<T>() where T : class
        => _settings.TryGetValue(typeof(T), out var value) ? (T)value
            : throw new InvalidOperationException($"Settings type {typeof(T).Name} is not registered.");
}
