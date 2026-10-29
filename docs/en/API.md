# Public API

[Русский](../API.md) · [English](../en/API.md) · [Deutsch](../de/API.md)

[Contents](README.md)

All types live in `Trexon.Configuration`. The following are signatures, not a standalone compilable file. The library creates `SettingsSourceConfiguration`, `SettingsManager` and `SettingsException`; their constructors are not public.

```csharp
public static class Settings
{
    public static T Load<T>(Action<SettingsConfiguration>? configure = null) where T : class;
}
public sealed class SettingsManager
{
    public static SettingsManager Create(Action<SettingsConfiguration> configure);
    public T Get<T>() where T : class;
}
public sealed class SettingsConfiguration
{
    public SettingsConfiguration();
    public SettingsSourceConfiguration ReadFrom { get; }
    public SettingsConfiguration Override(string key, string value);
    public SettingsConfiguration Register<T>() where T : class;
}
public sealed class SettingsSourceConfiguration
{
    public SettingsConfiguration EnvironmentVariables();
    public SettingsConfiguration DotEnv(string path, bool optional = false);
    public SettingsConfiguration InMemory(IReadOnlyDictionary<string, string> values);
}
public sealed class EnvAttribute : Attribute
{
    public EnvAttribute(string name);
    public string Name { get; }
}
public sealed class EnvPrefixAttribute : Attribute
{
    public EnvPrefixAttribute(string prefix);
    public string Prefix { get; }
}
public sealed class SecretAttribute : Attribute;
public enum SettingsErrorCode { Source, Model, Conversion, Validation }
public sealed record SettingsError(
    string PropertyPath, string? Key, string Source,
    SettingsErrorCode Code, string Message, bool IsSecret = false);
public sealed class SettingsException : Exception
{
    public IReadOnlyList<SettingsError> Errors { get; }
}
```

| API | Contract |
| --- | --- |
| `Settings.Load<T>` | New independent object per call; no global cache. Registrations are not used by this call |
| `SettingsManager.Create` | Requires at least one registration; reads sources once for all groups; eager loading |
| `Get<T>` | Registered types only; same instance on each call; concurrent manager reads are safe |
| `ReadFrom` | Adds sources from lower to higher priority; each method returns the main builder |
| `Override` | Exact key, no name conversion; always wins over sources; repeated calls replace values |
| `Register<T>` | Manager type registration; duplicate type registration fails |
| `Env` | Property only; exact key bypassing all prefixes; inherited on overrides |
| `EnvPrefix` | Class only, not inherited; literal prefix including `_` |
| `Secret` | Property sensitivity marker for errors; does not change values |
| `SettingsError` | Structured safe failure; never stores values |
| `SettingsException.Errors` | Immutable list of failures collected during the current load |

Use builders synchronously inside the callback. Mutating a captured builder after `Load/Create` does not affect loaded objects. Registrations apply to managers; standalone `Load<T>` loads only `T`. No Dispose, global Configure, reload or change events.

See [configuration](CONFIGURATION.md) for models, formats and sources, and [validation](VALIDATION.md) for rules and diagnostics.
