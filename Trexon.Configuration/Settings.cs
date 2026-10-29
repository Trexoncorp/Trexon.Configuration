using Trexon.Configuration.Internal;

namespace Trexon.Configuration;

/// <summary>Loads independent typed settings without maintaining global state.</summary>
public static class Settings
{
    /// <summary>Loads and validates a new instance. Reads process env by default; explicit sources replace that default.</summary>
    /// <remarks>Types need a public parameterless constructor. Register calls apply only to SettingsManager.</remarks>
    public static T Load<T>(Action<SettingsConfiguration>? configure = null) where T : class
    {
        var configuration = new SettingsConfiguration();
        configure?.Invoke(configuration);
        return (T)SettingsBinder.Load(typeof(T), configuration.ReadValues());
    }
}
