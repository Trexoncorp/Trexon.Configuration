# Validierung, Fehler und Geheimnisse

[Русский](../VALIDATION.md) · [English](../en/VALIDATION.md) · [Deutsch](../de/VALIDATION.md)

[Inhalt](README.md)

Nach Anwendung der Quellen werden die Werte validiert. Synchrone DataAnnotations-Regeln über `ValidationAttribute` werden unterstützt: etwa `[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]` und eigene Attribute. Auch Standardwerte werden validiert. Verschachtelte Gruppen werden rekursiv geprüft.

`[Required]` prüft null sowie leere Zeichenfolgen/Leerraum, nicht das Vorhandensein eines Umgebungsschlüssels. Ein nicht leerer Standardwert erfüllt die Regel. Für eine Pflichtzahl ohne Standardwert eine nullable-Eigenschaft verwenden: `[Required] public int? Port { get; init; }`. C# `required` und nullable-Annotationen ersetzen die Laufzeitvalidierung nicht.

<!-- verify: de-validation -->
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

## Regeln über mehrere Eigenschaften

Für zusammenhängende Eigenschaften `IValidatableObject` implementieren. `Validate(ValidationContext)` wird nach erfolgreicher Befüllung und Eigenschaftsvalidierung aufgerufen. Validierungsattribute an Klassen werden ebenfalls unterstützt. Keine Ressourcen oder Netzwerkaufrufe in Konstruktoren, Zugriffsmethoden und Validatoren laden: Die Bibliothek ruft diesen Anwendungscode synchron auf.

## Fehler

`SettingsException.Errors` ist eine schreibgeschützte Liste von `SettingsError`: Eigenschaftspfad, Schlüssel, Quelle, Phase, sichere Nachricht und Sensitivitätsflag. Phasen: `Source`, `Model`, `Conversion`, `Validation`. Wertquellen: `default`, `environment`, `memory`, `override`, `dotenv:<absolute path>`; Schemas und Objektregeln verwenden `model`.

Eigenschaftsfehler werden gesammelt. Manager sammeln Fehler aller registrierten Gruppen. Ein Quellenfehler stoppt das Lesen sofort; ein defekter Konstruktor oder ein ungültiges Schema verhindert die Eigenschaftsprüfung dieser Gruppe. Objektregeln werden bei bereits ungültigen Eigenschaften übersprungen. Teilweise befüllte Objekte oder Manager werden nicht zurückgegeben.

Ungültige Fluent-Argumente erzeugen `ArgumentException`/`ArgumentNullException`. Ein leerer Manager oder `Get<T>` für einen nicht registrierten Typ erzeugt `InvalidOperationException`.

## Geheimnisse

Die Bibliothek übernimmt keine Einstellungswerte in Nachrichten, `SettingsError` oder innere Ausnahmen. Auch eigene Validator- und Ausnahmemeldungen werden nicht kopiert: Diagnosen nennen die Regel oder Fehlerphase. Schlüssel, Eigenschaftspfade und dotenv-Pfade bleiben sichtbar; darin keine Geheimnisse ablegen.

`[Secret]` setzt `IsSecret` in den Fehlermetadaten. Bei verschachtelten Gruppen wird das Flag an ihre Eigenschaften weitergegeben. Es verschlüsselt oder maskiert das Objekt nicht: Die Anwendung erhält den echten Schlüssel. `JsonSerializer.Serialize(settings)`, Protokollieren des Objekts und eigenes `GetVapidKeys()` können Geheimnisse offenlegen. Nur ausgewählte unkritische Felder protokollieren. Seiteneffekte im eigenen Modellcode werden nicht kontrolliert.

Git ignoriert `.env` und `.env.*`, außer `.env.example`. `sample.env` darf nur Demonstrationswerte enthalten.
