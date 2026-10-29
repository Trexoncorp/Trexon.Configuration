# Trexon.Configuration

[English](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.md) · [Русский](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.ru.md) · [Deutsch](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.de.md)

Typed configuration for C# / .NET 8+: classes with defaults, automatic env loading, aliases, prefixes, dotenv, validation and independent managers. No external runtime NuGet dependencies. MIT licensed.

**Version `1.0.0`** — the first stable release. Public API compatibility is maintained within 1.x.

## Start with one class

<!-- verify: en-readme-basic -->
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

Plain `new AppSettings()` uses defaults. `Settings.Load<AppSettings>()` creates a new object, reads env and validates values. There is no automatic reload.

## Settings groups

<!-- verify: en-readme-manager -->
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

Install the package from NuGet:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

## Documentation

- [Contents](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/README.md)
- [Getting started](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/GETTING_STARTED.md)
- [Sources and models](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/CONFIGURATION.md)
- [Validation and secrets](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/VALIDATION.md)
- [API reference](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/API.md)
- [Architecture](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/ARCHITECTURE.md)
- [Development and tests](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/DEVELOPMENT.md)
- [Release preparation and publishing](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/PUBLISHING.md)
- [Version 1.0 scope](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/en/ROADMAP.md)
