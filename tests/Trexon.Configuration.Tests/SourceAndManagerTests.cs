namespace Trexon.Configuration.Tests;

[Collection("Process globals")]
public sealed class SourceAndManagerTests
{
    [Fact]
    public void Default_source_is_process_env_and_manager_values_are_stable()
    {
        const string key = "TREXON_SETTINGS_TEST_PORT";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "9001");
            Assert.Equal(9001, Settings.Load<ProcessModel>().Port);
            var manager = SettingsManager.Create(c => c.Register<ProcessModel>());
            Environment.SetEnvironmentVariable(key, "9002");
            Assert.Equal(9001, manager.Get<ProcessModel>().Port);
            Assert.Equal(9002, Settings.Load<ProcessModel>().Port);
        }
        finally { Environment.SetEnvironmentVariable(key, previous); }
    }

    [Fact]
    public void Explicit_sources_replace_default_env_and_overrides_always_win()
    {
        const string key = "TREXON_SETTINGS_TEST_PORT";
        var previous = Environment.GetEnvironmentVariable(key);
        using var temp = new TemporaryDirectory();
        var path = temp.File(key + "=8001");
        try
        {
            Environment.SetEnvironmentVariable(key, "8002");
            Assert.Equal(8001, Settings.Load<ProcessModel>(c => c.ReadFrom.DotEnv(path)).Port);
            Assert.Equal(8002, Settings.Load<ProcessModel>(c => c.ReadFrom.DotEnv(path).ReadFrom.EnvironmentVariables()).Port);
            Assert.Equal(8003, Settings.Load<ProcessModel>(c => c.Override(key, "8003")
                .ReadFrom.DotEnv(path).ReadFrom.EnvironmentVariables()).Port);
            Assert.Equal("8002", Environment.GetEnvironmentVariable(key));
        }
        finally { Environment.SetEnvironmentVariable(key, previous); }
    }

    [Fact]
    public void Manager_eagerly_validates_all_groups_and_returns_the_same_instance()
    {
        var manager = SettingsManager.Create(c => c.ReadFrom.InMemory(new Dictionary<string, string>())
            .Register<TextModel>().Register<ProcessModel>());
        Assert.Same(manager.Get<TextModel>(), manager.Get<TextModel>());
        Parallel.For(0, 100, _ => Assert.Equal("default", manager.Get<TextModel>().Text));
        Assert.Throws<InvalidOperationException>(() => manager.Get<BindingTests.App>());
        var failure = Assert.Throws<SettingsException>(() => SettingsManager.Create(c => c
            .ReadFrom.InMemory(new Dictionary<string, string> { ["PORT"] = "bad", ["TOKEN"] = "" })
            .Register<ValidationTests.Validated>().Register<BindingTests.Cycle>()));
        Assert.Contains(failure.Errors, e => e.Code == SettingsErrorCode.Conversion);
        Assert.Contains(failure.Errors, e => e.Code == SettingsErrorCode.Model);
    }

    [Fact]
    public void Dictionary_inputs_and_captured_builders_cannot_mutate_loaded_managers()
    {
        var input = new Dictionary<string, string> { ["TEXT"] = "original" };
        SettingsConfiguration captured = null!;
        var manager = SettingsManager.Create(c =>
        {
            captured = c;
            c.ReadFrom.InMemory(input).Register<TextModel>();
            input["TEXT"] = "changed";
        });
        captured.Override("TEXT", "later");
        Assert.Equal("original", manager.Get<TextModel>().Text);
        Assert.Equal("independent", SettingsManager.Create(c => c.Override("TEXT", "independent")
            .Register<TextModel>()).Get<TextModel>().Text);
    }

    [Theory]
    [InlineData("TEXT=plain", "plain")]
    [InlineData("export TEXT = 'привет # literal'", "привет # literal")]
    [InlineData("TEXT=abc#literal", "abc#literal")]
    [InlineData("TEXT=abc # comment", "abc")]
    [InlineData("TEXT= # comment", "")]
    [InlineData("TEXT=", "")]
    [InlineData("TEXT=\"a\\nb\\t\\\"c\\\\\" # comment", "a\nb\t\"c\\")]
    [InlineData("TEXT='${OTHER}'", "${OTHER}")]
    [InlineData("TEXT=first\nTEXT=last", "last")]
    [InlineData("# comment\n\nTEXT = second=value", "second=value")]
    public void Dotenv_handles_documented_syntax(string content, string expected)
    {
        using var temp = new TemporaryDirectory();
        var path = temp.File(content);
        Assert.Equal(expected, Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path)).Text);
    }

    [Theory]
    [InlineData("TEXT='secret-marker")]
    [InlineData("TEXT=\"secret-marker\"garbage")]
    [InlineData("TEXT=\"secret-marker\\q\"")]
    [InlineData("invalid-key=secret-marker")]
    [InlineData("secret-marker")]
    public void Malformed_dotenv_is_not_ignored_even_when_optional_and_never_echoes_content(string content)
    {
        using var temp = new TemporaryDirectory();
        var path = temp.File("# first\n" + content);
        var failure = Assert.Throws<SettingsException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path, optional: true)));
        Assert.Equal(SettingsErrorCode.Source, Assert.Single(failure.Errors).Code);
        Assert.Contains("line 2", failure.Message);
        Assert.DoesNotContain("secret-marker", failure.ToString());
    }

    [Fact]
    public void Only_optional_missing_files_are_ignored()
    {
        using var temp = new TemporaryDirectory();
        var path = System.IO.Path.Combine(temp.Path, "missing", ".env");
        Assert.Equal("default", Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path, optional: true)).Text);
        Assert.Throws<SettingsException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path)));
        Assert.Throws<SettingsException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(temp.Path, optional: true)));
    }

    [Fact]
    public void Dotenv_accepts_utf8_bom_and_rejects_invalid_utf8()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.File("TEXT=привет");
        File.WriteAllText(path, "TEXT=привет", new System.Text.UTF8Encoding(true));
        Assert.Equal("привет", Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path)).Text);
        File.WriteAllBytes(path, [0x54, 0x45, 0x58, 0x54, 0x3D, 0xFF]);
        Assert.Throws<SettingsException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path)));
        File.WriteAllText(path, "TEXT=wrong encoding", System.Text.Encoding.Unicode);
        Assert.Throws<SettingsException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(path)));
    }

    [Fact]
    public void Bad_configuration_is_rejected_immediately()
    {
        Assert.Throws<InvalidOperationException>(() => SettingsManager.Create(_ => { }));
        Assert.Throws<ArgumentException>(() => SettingsManager.Create(c => c.Register<TextModel>().Register<TextModel>()));
        Assert.Throws<ArgumentException>(() => Settings.Load<TextModel>(c => c.Override("invalid-key", "x")));
        Assert.Throws<ArgumentException>(() => Settings.Load<TextModel>(c => c.ReadFrom.DotEnv(" ")));
        Assert.Throws<ArgumentNullException>(() => SettingsManager.Create(null!));
    }

    [Fact]
    public void Manager_reads_process_env_before_any_model_constructor_runs()
    {
        const string key = "TREXON_SETTINGS_TEST_PORT";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "9001");
            var manager = SettingsManager.Create(c => c.Register<MutatesEnv>().Register<ProcessModel>());
            Assert.Equal(9001, manager.Get<ProcessModel>().Port);
            Assert.Equal("9002", Environment.GetEnvironmentVariable(key));
        }
        finally { Environment.SetEnvironmentVariable(key, previous); }
    }

    public sealed class ProcessModel { [Env("TREXON_SETTINGS_TEST_PORT")] public int Port { get; init; } = 8000; }
    public sealed class TextModel { public string Text { get; init; } = "default"; }
    public sealed class MutatesEnv
    {
        public MutatesEnv() => Environment.SetEnvironmentVariable("TREXON_SETTINGS_TEST_PORT", "9002");
    }
}
