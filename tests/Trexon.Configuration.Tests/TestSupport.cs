namespace Trexon.Configuration.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "trexon-settings-" + Guid.NewGuid().ToString("N"));
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public string File(string content)
    {
        var path = System.IO.Path.Combine(Path, ".env");
        System.IO.File.WriteAllText(path, content);
        return path;
    }
    public void Dispose() => Directory.Delete(Path, true);
}

[CollectionDefinition("Process globals", DisableParallelization = true)]
public sealed class ProcessGlobalsCollection;

internal static class TestSettings
{
    internal static T Load<T>(params (string Key, string Value)[] pairs) where T : class
        => Settings.Load<T>(c => c.ReadFrom.InMemory(pairs.ToDictionary(p => p.Key, p => p.Value)));
}
