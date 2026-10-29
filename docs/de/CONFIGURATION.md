# Quellen und Modelle

[Русский](../CONFIGURATION.md) · [English](../en/CONFIGURATION.md) · [Deutsch](../de/CONFIGURATION.md)

[Inhalt](README.md)

## Quellen und Priorität

Ohne explizite Quellen wird die Prozessumgebung gelesen. `Override` deaktiviert diesen Standard nicht. Der erste Aufruf von `ReadFrom.*` ersetzt den Standard durch eine explizite Liste. Quellen werden in Registrierungsreihenfolge gelesen; die letzte gewinnt. Overrides haben immer höchste Priorität.

Typische Kette: `config.ReadFrom.DotEnv(".env", optional: true).ReadFrom.EnvironmentVariables().Override("APP_PORT", "9000")`. Priorität: Override → Umgebung → dotenv → Eigenschaftsinitialisierer. Ein ungültiger vorrangiger Wert führt zum Fehler; es gibt keinen Rückgriff auf niedrigere Quellen.

Schlüssel verwenden `StringComparer.Ordinal`: Groß-/Kleinschreibung wird auf allen Plattformen beachtet. Generierte Schlüssel sind großgeschrieben. Unbekannte Schlüssel werden ignoriert. Zwei Eigenschaften eines Modells dürfen nicht denselben Schlüssel verwenden; getrennte registrierte Modelle dürfen Schlüssel teilen.

`InMemory(IReadOnlyDictionary<string, string>)` kopiert die Eingabe beim Hinzufügen. Null-Werte werden abgelehnt. Dotenv-Pfade werden beim Hinzufügen relativ zum aktuellen Arbeitsverzeichnis absolut aufgelöst; gelesen wird beim Laden. Dateien und Prozessumgebung werden nicht verändert.

## Namen und Modelle

`ProjectName` → `PROJECT_NAME`, `APIPrefix` → `API_PREFIX`, `DbPoolSize` → `DB_POOL_SIZE`, `HTTP2Port` → `HTTP2_PORT`. Vorhandene Unterstriche bleiben erhalten. Generierte und explizite Schlüssel müssen `[A-Za-z_][A-Za-z0-9_]*` entsprechen.

`[Env("APP_HOST")]` ist der vollständige Schlüssel und umgeht alle Präfixe. `[EnvPrefix("VAPID_")]` gilt nur für die annotierte Klasse; Präfixe von Basisklassen werden nicht geerbt. Ein abschließendes `_` wird nicht automatisch ergänzt. Leere Präfixe sind erlaubt.

Geerbte öffentliche Eigenschaften werden geladen. Methoden, statische Eigenschaften, Indexer und nicht annotierte schreibgeschützte Eigenschaften werden ignoriert. Eine schreibgeschützte Eigenschaft mit Env, Secret oder Validierungsattributen macht das Modell ungültig.

Verschachtelte Gruppen verwenden `__`. Jede Ebene ergänzt das Typpräfix: Wurzel `[EnvPrefix("APP_")]` + `Database` + verschachteltes `[EnvPrefix("DB_")]` ergibt `APP_DATABASE__DB_POOL_SIZE`. Für `DATABASE__POOL_SIZE` sind keine Präfixe nötig. Env-Aliase gelten nur für skalare Eigenschaften.

<!-- verify: de-nested -->
```csharp
using Trexon.Configuration;

var settings = Settings.Load<AppSettings>(config => config
    .ReadFrom.InMemory(new Dictionary<string, string> { ["DATABASE__POOL_SIZE"] = "5" }));
if (settings.Database.PoolSize != 5) throw new Exception("Pool mismatch");

public sealed class AppSettings : CommonSettings
{
    public DatabaseSettings Database { get; init; } = new();
}
public abstract class CommonSettings
{
    public string ProjectName { get; init; } = "Widget iSpace";
}
public sealed class DatabaseSettings
{
    public int PoolSize { get; init; } = 3;
}
```

Initialisierte Gruppen behalten ihre Standardwerte. Eine null-Gruppe wird nur erzeugt, wenn einer ihrer bekannten Schlüssel vorhanden ist. Sonst bleibt sie null; für Pflichtgruppen `[Required]` verwenden.

Sammlungen, Wörterbücher, beliebige structs, interface/abstract-Gruppen und Konstruktoren ohne öffentliche parameterlose Variante werden nicht unterstützt. Modellzyklen werden abgelehnt; die Tiefe ist auf 32 Klassen begrenzt. Keine gemeinsam genutzten statischen Objekte als Initialisierer verwenden: Der Lader befüllt das vom Modell erstellte Objekt.

## Konvertierung

| Typ | Format |
| --- | --- |
| `string` | Unveränderte Zeichenfolge |
| `bool` | `true`/`false` ohne Beachtung der Großschreibung; `1`/`0` |
| `byte/sbyte/short/ushort/int/uint/long/ulong` | Ganzzahl mit Bereichsprüfung |
| `float/double/decimal` | Invariant culture, Dezimalpunkt, optionaler Exponent; keine Tausendertrenner |
| `float/double` | Nur endliche Werte; NaN/Infinity werden abgelehnt |
| enum | Namen ohne Beachtung der Großschreibung; keine Zahlen oder Flag-Kombinationen |
| `Guid` | Von `Guid.Parse` unterstützte Formate |
| `Uri` | Wohlgeformter absoluter URI; keine relativen Pfade |
| `TimeSpan` | Standardformat `c`: `00:00:00.2000000` |
| `DateTime`, `DateTimeOffset` | Round-trip `O`, einschließlich 7 Nachkommastellen für Sekunden |
| `DateOnly` | `yyyy-MM-dd` |
| `TimeOnly` | `HH:mm:ss`, optional bis zu 7 Nachkommastellen |
| `Nullable<T>` | Dieselben Formate; leere Zeichenfolge bedeutet null |

Fehlende Schlüssel behalten Standardwerte. Leere Zeichenfolgen bleiben für `string` leer, sind für nicht nullable Zahlen aber ungültig. `null` als Text hat keine Sonderbedeutung. `string?` ersetzt `[Required]` nicht; nullable-Annotationen werden zur Laufzeit nicht erzwungen.

## Dotenv-Teilmenge

UTF-8 mit optionalem BOM. Leerzeilen, `#`-Kommentare, `export KEY=value`, Leerzeichen um `=`, leere Werte sowie einfache und doppelte Anführungszeichen werden unterstützt. Der letzte doppelte Schlüssel gewinnt.

```dotenv
APP_PORT=8080
PROJECT_NAME="Widget iSpace"
TOKEN='literal # text'
HOST=localhost # comment
VALUE=abc#literal
```

Ohne Anführungszeichen beginnt ein Inline-Kommentar mit `#` am Wertanfang oder nach Leerraum. Nach einem schließenden Anführungszeichen ist nur ein durch Leerraum getrennter Kommentar erlaubt. Einfache Anführungszeichen sind wörtlich. Doppelte unterstützen `\n`, `\r`, `\t`, `\\`, `\"`; unbekannte Escapes sind Fehler.

Keine `${NAME}`-Interpolation, Shell-Ausführung, mehrzeiligen Werte oder Zeilenfortsetzung. `${NAME}` bleibt wörtlich. Syntaxfehler enthalten die Zeilennummer ohne Zeileninhalt. Ungültiges UTF-8 erzeugt einen Kodierungsfehler ohne Rohdaten; UTF-16/UTF-32 werden nicht unterstützt. `optional: true` ignoriert nur fehlende Dateien/Verzeichnisse, keine Syntax- oder Zugriffsfehler. Fehler einer registrierten Quelle brechen das Laden auch dann ab, wenn ihre Werte später überschrieben würden.
