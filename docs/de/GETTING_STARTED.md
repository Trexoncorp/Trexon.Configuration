# Erste Schritte

[Русский](../GETTING_STARTED.md) · [English](../en/GETTING_STARTED.md) · [Deutsch](../de/GETTING_STARTED.md)

[Inhalt](README.md)

Anwendungen benötigen .NET 8 oder neuer. Für die Entwicklung: SDK 10 sowie die Laufzeitumgebungen .NET 8 und .NET 10. Der Kern hängt weder von ASP.NET noch von DI oder Trexon.Logging ab.

Von NuGet installieren:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

Version `1.0.0` ist die erste stabile Version.

Modelle benötigen einen öffentlichen parameterlosen Konstruktor. Einstellungen sind öffentliche Eigenschaften mit `get; init;` oder `get; set;`. `init` verhindert spätere Änderungen durch Anwendungscode.

<!-- verify: de-start -->
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

In der startenden Shell `APP_PORT=9000` setzen: `Port` wird `9000`. Ohne die Variable bleibt der Wert `8000`. Die Umgebung wird einmal beim Laden gelesen, nicht bei jedem Zugriff.

Ein Manager lädt mehrere Gruppen. `InMemory` kopiert die Eingabe beim Hinzufügen und isoliert dieses Beispiel von der Prozessumgebung.

<!-- verify: de-groups -->
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

Das vollständige Widget/VAPID-Beispiel steht unter `examples/Trexon.Configuration.Sample/`. Im Repository-Stamm:

```sh
dotnet run --project examples/Trexon.Configuration.Sample
```

Der Build kopiert `sample.env` mit Demonstrationswerten neben die ausführbare Datei. Prozessvariablen haben Vorrang. Der private Schlüssel wird nicht ausgegeben. Mit `APP_PORT=70000` schlägt die Validierung fehl; das Beispiel endet mit Code 1.

Für Trexon.Logging zuerst die Einstellungen laden und dann den gewünschten Level an `Logger.Create` übergeben. Die Bibliotheken sind unabhängig; Zeichenfolgen für Levels werden von der Anwendung umgewandelt.
