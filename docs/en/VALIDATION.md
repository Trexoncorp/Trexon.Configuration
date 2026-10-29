# Validation, errors and secrets

[Русский](../VALIDATION.md) · [English](../en/VALIDATION.md) · [Deutsch](../de/VALIDATION.md)

[Contents](README.md)

Validation runs after applying sources. Synchronous DataAnnotations `ValidationAttribute` rules are supported, including `[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]` and custom attributes. Defaults are validated too. Nested groups are checked recursively.

`[Required]` checks null and empty/whitespace strings, not whether an env key exists. A nonempty default satisfies it. For a mandatory number without a default, use a nullable property such as `[Required] public int? Port { get; init; }`. C# `required` and nullable annotations do not replace runtime validation.

<!-- verify: en-validation -->
```csharp
using System.ComponentModel.DataAnnotations;
using Trexon.Configuration;

try
{
    Settings.Load<AppSettings>(config => config
        .ReadFrom.InMemory(new Dictionary<string, string> { ["APP_PORT"] = "70000" }));
    throw new Exception("Expected validation failure");
}
catch (SettingsException exception)
{
    if (exception.Errors.Count != 2) throw;
    Console.WriteLine(exception.Message);
}

public sealed class AppSettings
{
    [Env("APP_PORT"), Range(1, 65535)] public int Port { get; init; } = 8000;
    [Required, Secret] public string Token { get; init; } = "";
}
```

## Cross-property rules

Implement `IValidatableObject` for related properties. Its `Validate(ValidationContext)` runs after successful binding and property validation. Class validation attributes are also supported. Avoid resource loading and network calls in constructors, accessors and validators; they are synchronous user code invoked by the library.

## Failures

`SettingsException.Errors` is a read-only list of `SettingsError`: property path, key, source, stage, safe message and sensitivity flag. Stages: `Source`, `Model`, `Conversion`, `Validation`. Value sources: `default`, `environment`, `memory`, `override`, `dotenv:<absolute path>`; schema and object rules use `model`.

Property failures are aggregated. Managers collect failures from all registered groups. A source failure stops reading immediately; a broken constructor or schema prevents property validation for that group. Object rules are skipped when properties have already failed. No partially populated object or manager is returned.

Invalid fluent arguments throw `ArgumentException`/`ArgumentNullException`. An empty manager or `Get<T>` for an unregistered type throws `InvalidOperationException`.

## Secrets

The library does not include setting values in its messages, `SettingsError` or inner exceptions. User validator and exception messages are not copied either: diagnostics identify the rule or failed stage. Key names, property paths and dotenv paths remain visible; never embed secrets in those identifiers.

`[Secret]` marks property error metadata with `IsSecret`. On a nested group the flag propagates to its properties. It neither encrypts nor masks the object: the application receives the real key. `JsonSerializer.Serialize(settings)`, logging the object and user-defined `GetVapidKeys()` can expose secrets. Log only selected nonsensitive fields. The library cannot control side effects in user model code.

Git ignores `.env` and `.env.*`, except `.env.example`. Only demonstration values belong in `sample.env`.
