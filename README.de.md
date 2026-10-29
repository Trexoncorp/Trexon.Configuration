# Trexon.Configuration

[English](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.md) · [Русский](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.ru.md) · [Deutsch](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.de.md)

Typisierte Konfiguration für C# / .NET 8+: Klassen mit Standardwerten, automatisches Laden von Umgebungsvariablen, Aliase, Präfixe, dotenv, Validierung und unabhängige Manager. Keine externen NuGet-Abhängigkeiten zur Laufzeit. MIT-Lizenz.

**Version `1.0.0`** — die erste stabile Version. Die öffentliche API bleibt innerhalb von 1.x kompatibel.

## Mit einer Klasse beginnen

<!-- verify: de-readme-basic -->
```csharp
using Trexon.Configuration;

var settings = Settings.Load<AppSettings>();
Console.WriteLine($"{settings.ProjectName}: {settings.Port}");

public sealed class AppSettings
{
    public string ProjectName { get; init; } = "Widget iSpace";
    [Env("APP_PORT")] public int Port { get; init; } = 8000;
}
```

`new AppSettings()` verwendet Standardwerte. `Settings.Load<AppSettings>()` erstellt ein neues Objekt, liest Umgebungsvariablen und validiert die Werte. Es gibt kein automatisches Neuladen.

## Einstellungsgruppen

<!-- verify: de-readme-manager -->
```csharp
using Trexon.Configuration;

var manager = SettingsManager.Create(config => config
    .ReadFrom.InMemory(new Dictionary<string, string> { ["VAPID_PUBLIC_KEY"] = "demo" })
    .Override("APP_PORT", "9000")
    .Register<AppSettings>()
    .Register<VapidSettings>());

if (manager.Get<AppSettings>().Port != 9000) throw new Exception("Port mismatch");
if (manager.Get<VapidSettings>().PublicKey != "demo") throw new Exception("Key mismatch");

public sealed class AppSettings
{
    [Env("APP_PORT")] public int Port { get; init; } = 8000;
}

[EnvPrefix("VAPID_")]
public sealed class VapidSettings
{
    public string PublicKey { get; init; } = "";
    [Secret] public string PrivateKey { get; init; } = "";
}
```

## Installation

Das Paket von NuGet installieren:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

## Dokumentation

- [Inhalt](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/README.md)
- [Erste Schritte](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/GETTING_STARTED.md)
- [Quellen und Modelle](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/CONFIGURATION.md)
- [Validierung und Geheimnisse](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/VALIDATION.md)
- [API-Referenz](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/API.md)
- [Architektur](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/ARCHITECTURE.md)
- [Entwicklung und Tests](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/DEVELOPMENT.md)
- [Vorbereitung und Veröffentlichung](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/PUBLISHING.md)
- [Umfang von Version 1.0](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/de/ROADMAP.md)
