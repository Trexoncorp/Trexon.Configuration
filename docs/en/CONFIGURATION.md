# Sources and models

[Русский](../CONFIGURATION.md) · [English](../en/CONFIGURATION.md) · [Deutsch](../de/CONFIGURATION.md)

[Contents](README.md)

## Sources and precedence

Without explicit sources, process env is read. `Override` does not disable this default. The first `ReadFrom.*` call replaces the default with an explicit list. Sources are read in registration order; the last wins. Overrides always have highest priority, regardless of their position in the chain.

Typical setup: `config.ReadFrom.DotEnv(".env", optional: true).ReadFrom.EnvironmentVariables().Override("APP_PORT", "9000")`. Priority: override → env → dotenv → property initializers. An invalid winning value fails loading; there is no fallback to a lower source.

Keys use `StringComparer.Ordinal`: case-sensitive on every platform. Use uppercase generated keys. Unknown keys are ignored because process env contains unrelated variables. Two properties in one model cannot map to the same key; separate registered models may share keys.

`InMemory(IReadOnlyDictionary<string, string>)` copies inputs when added. Null values are rejected. Dotenv paths become absolute when added, relative to the current working directory; files are read during loading. Neither files nor process env are modified.

## Names and models

`ProjectName` → `PROJECT_NAME`, `APIPrefix` → `API_PREFIX`, `DbPoolSize` → `DB_POOL_SIZE`, `HTTP2Port` → `HTTP2_PORT`. Existing underscores are preserved. Generated and explicit keys must match `[A-Za-z_][A-Za-z0-9_]*`.

`[Env("APP_HOST")]` is an exact key, bypassing all prefixes. `[EnvPrefix("VAPID_")]` applies only to the annotated class; base-class prefixes are not inherited. A trailing `_` is not added automatically. Empty prefixes are allowed.

Inherited public properties are loaded. Methods, static properties, indexers and unannotated read-only properties are ignored. A read-only property annotated with Env, Secret or validation attributes makes the model invalid.

Nested groups use `__`. Each level adds its type prefix: root `[EnvPrefix("APP_")]` + `Database` + nested `[EnvPrefix("DB_")]` produces `APP_DATABASE__DB_POOL_SIZE`. No prefixes are needed for plain `DATABASE__POOL_SIZE`. Env aliases apply only to scalar properties.

<!-- verify: en-nested -->
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

Initialized nested groups preserve their defaults. A null group is created only when one of its known keys is supplied. Otherwise it stays null; use `[Required]` for a mandatory group.

Collections, dictionaries, arbitrary structs, interface/abstract nested types and constructors without a public parameterless overload are unsupported. Model cycles are rejected; depth is limited to 32 classes. Do not use shared static objects as nested initializers: loading populates the object created by the model.

## Conversion

| Type | Format |
| --- | --- |
| `string` | Original string |
| `bool` | Case-insensitive `true`/`false`; `1`/`0` |
| `byte/sbyte/short/ushort/int/uint/long/ulong` | Integer, checked range |
| `float/double/decimal` | Invariant culture, decimal point, optional exponent; no grouping separators |
| `float/double` | Finite values only; NaN/Infinity are rejected |
| enum | Case-insensitive names only; no numeric values or flag combinations |
| `Guid` | Formats accepted by `Guid.Parse` |
| `Uri` | Well-formed absolute URI; relative paths are rejected |
| `TimeSpan` | Standard `c` format: `00:00:00.2000000` |
| `DateTime`, `DateTimeOffset` | Round-trip `O`, including 7 fractional second digits |
| `DateOnly` | `yyyy-MM-dd` |
| `TimeOnly` | `HH:mm:ss` with up to 7 optional fractional digits |
| `Nullable<T>` | Same formats; empty string means null |

Missing keys keep defaults. An empty string stays empty for `string`, but fails for non-nullable numbers. The literal `null` has no special meaning. `string?` does not replace `[Required]`; nullable annotations are not enforced at runtime.

## Dotenv subset

UTF-8, optional BOM. Supports blank lines, `#` comments, `export KEY=value`, spaces around `=`, empty values, single and double quotes. The last duplicate key wins.

```dotenv
APP_PORT=8080
PROJECT_NAME="Widget iSpace"
TOKEN='literal # text'
HOST=localhost # comment
VALUE=abc#literal
```

Unquoted inline comments start with `#` at the start of the value or after whitespace. After a closing quote, only a whitespace-separated comment is allowed. Single quotes are literal. Double quotes support `\n`, `\r`, `\t`, `\\`, `\"`; unknown escapes fail.

No `${NAME}` interpolation, shell execution, multiline values or line continuation. `${NAME}` stays literal. Syntax failures include the line number without its contents. Invalid UTF-8 produces an encoding error without raw data; UTF-16/UTF-32 are unsupported. `optional: true` ignores missing files/directories only, never syntax or access failures. A failed registered source aborts loading even if its values would have been overridden.
