# Öffentliche API

[Русский](../API.md) · [English](../en/API.md) · [Deutsch](../de/API.md)

[Inhalt](README.md)

Alle Typen liegen in `Trexon.Configuration`. Die folgenden Signaturen sind keine eigenständig kompilierbare Datei. `SettingsSourceConfiguration`, `SettingsManager` und `SettingsException` werden von der Bibliothek erstellt; ihre Konstruktoren sind nicht öffentlich.

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

| API | Vertrag |
| --- | --- |
| `Settings.Load<T>` | Neues unabhängiges Objekt pro Aufruf; kein globaler Cache. Registrierungen werden hier nicht verwendet |
| `SettingsManager.Create` | Mindestens eine Registrierung; Quellen werden einmal für alle Gruppen gelesen; sofortiges Laden |
| `Get<T>` | Nur registrierte Typen; immer dieselbe Instanz; parallele Manager-Lesezugriffe sind sicher |
| `ReadFrom` | Quellen von niedriger zu höherer Priorität; jede Methode liefert den Haupt-Builder |
| `Override` | Exakter Schlüssel ohne Namensumwandlung; hat immer Vorrang; wiederholte Aufrufe ersetzen Werte |
| `Register<T>` | Typregistrierung für Manager; doppelte Registrierung schlägt fehl |
| `Env` | Nur Eigenschaften; exakter Schlüssel ohne Präfixe; bei Overrides geerbt |
| `EnvPrefix` | Nur Klassen, nicht geerbt; wörtliches Präfix einschließlich `_` |
| `Secret` | Sensitivitätsmarkierung für Fehler; verändert Werte nicht |
| `SettingsError` | Strukturierter sicherer Fehler; speichert keine Werte |
| `SettingsException.Errors` | Unveränderliche Liste der beim Laden gesammelten Fehler |

Builder synchron im Callback verwenden. Änderungen am gespeicherten Builder nach `Load/Create` beeinflussen geladene Objekte nicht. Registrierungen gelten für Manager; `Load<T>` lädt nur `T`. Kein Dispose, globales Configure, Reload oder Änderungsereignis.

Modelle, Formate und Quellen: [Konfiguration](CONFIGURATION.md). Regeln und Diagnosen: [Validierung](VALIDATION.md).
