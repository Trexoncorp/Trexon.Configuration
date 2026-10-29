# Getting started

[Русский](../GETTING_STARTED.md) · [English](../en/GETTING_STARTED.md) · [Deutsch](../de/GETTING_STARTED.md)

[Contents](README.md)

Consumers need .NET 8 or later. Repository development needs SDK 10 and both .NET 8 and .NET 10 runtimes. The core does not depend on ASP.NET, DI or Trexon.Logging.

Install from NuGet:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

Version `1.0.0` is the first stable release.

Models need a public parameterless constructor. Settings are public properties with `get; init;` or `get; set;`. Prefer `init` to prevent application code from mutating loaded settings.

<!-- verify: en-start -->
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

Set `APP_PORT=9000` in the launching shell: `Port` becomes `9000`. Without the variable it remains `8000`. Env is read once during `Load`, not on each property access.

Use a manager for multiple groups. `InMemory` copies its input when added, isolating this example from process env.

<!-- verify: en-groups -->
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

The complete Widget/VAPID example lives in `examples/Trexon.Configuration.Sample/`. From the repository root:

```sh
dotnet run --project examples/Trexon.Configuration.Sample
```

The build copies `sample.env` with demonstration values next to the executable. Process env wins over the file. The private key is never printed. Run with `APP_PORT=70000` to see validation fail; the sample exits with code 1.

For Trexon.Logging integration, load settings first, then pass the desired level to `Logger.Create`. The libraries are independent; the application converts its own level strings.
