using System.ComponentModel.DataAnnotations;
using Trexon.Configuration;

// Read sample.env next to the executable; it contains demonstration values only.
// Real process env overrides the file. No process environment is modified by dotenv loading.
try
{
    var manager = SettingsManager.Create(config => config
        .ReadFrom.DotEnv(Path.Combine(AppContext.BaseDirectory, "sample.env"))
        .ReadFrom.EnvironmentVariables()
        .Register<AppSettings>()
        .Register<VapidSettings>());

    var app = manager.Get<AppSettings>();
    var vapid = manager.Get<VapidSettings>();
    Console.WriteLine($"{app.ProjectName}: http://{app.Host}:{app.Port}{app.ApiPrefix}");
    Console.WriteLine($"Database pool: {app.DbPoolSize}; overload timeout: {app.AppOverloadTimeoutSeconds}s");
    Console.WriteLine($"VAPID keys supplied: {vapid.PublicKey.Length > 0 && vapid.PrivateKey.Length > 0}");
}
catch (SettingsException exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}

public abstract class DatabaseSettings
{
    [Range(1, 1000)] public int DbPoolSize { get; init; } = 3;
    [Range(0, 1000)] public int DbMaxOverflow { get; init; } = 7;
    [Range(1, 3600)] public int DbPoolTimeout { get; init; } = 60;
}

public sealed class AppSettings : DatabaseSettings
{
    public string ProjectName { get; init; } = "Example";
    [Env("APP_HOST")] public string Host { get; init; } = "0.0.0.0";
    [Env("APP_PORT"), Range(1, 65535)] public int Port { get; init; } = 8000;
    public string SwaggerPathPrefix { get; init; } = "/widget";
    public string LoggerLevel { get; init; } = "INFO";
    public string ApiPrefix { get; init; } = "/api/v1";
    [Range(0, int.MaxValue)] public int AppMaxConcurrentRequests { get; init; }
    [Range(0.001, 60)] public double AppOverloadTimeoutSeconds { get; init; } = 0.2;
    public int WidgetRefreshStatusTtlSeconds { get; init; } = 3600;
    public int WidgetRefreshLockTtlSeconds { get; init; } = 900;
}

[EnvPrefix("VAPID_")]
public sealed class VapidSettings
{
    [Required] public string PublicKey { get; init; } = "";
    [Required, Secret] public string PrivateKey { get; init; } = "";
    public Dictionary<string, string> GetVapidKeys() => new()
    {
        ["publicKey"] = PublicKey,
        ["privateKey"] = PrivateKey
    };
}
