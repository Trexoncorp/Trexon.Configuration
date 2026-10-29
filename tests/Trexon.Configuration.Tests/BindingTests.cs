using System.ComponentModel.DataAnnotations;

namespace Trexon.Configuration.Tests;

public sealed class BindingTests
{
    [Fact]
    public void Defaults_aliases_prefixes_inheritance_and_methods_work_together()
    {
        var app = TestSettings.Load<App>(("APP_PORT", "9000"), ("DB_POOL_SIZE", "5"));
        Assert.Equal("Widget iSpace", app.ProjectName);
        Assert.Equal(9000, app.Port);
        Assert.Equal(5, app.DbPoolSize);
        var vapid = TestSettings.Load<Vapid>(("VAPID_PUBLIC_KEY", "public"), ("EXACT_PRIVATE_KEY", "private"));
        Assert.Equal("public", vapid.GetKeys()["publicKey"]);
        Assert.Equal("private", vapid.PrivateKey);
        Assert.Equal("computed", vapid.Computed);
    }

    [Fact]
    public void Acronyms_and_existing_underscores_have_deterministic_names()
    {
        var value = TestSettings.Load<Names>(("API_PREFIX", "/v2"), ("DB_POOL_SIZE", "5"), ("HTTP2_PORT", "8080"));
        Assert.Equal("/v2", value.APIPrefix);
        Assert.Equal(5, value.DB_POOL_SIZE);
        Assert.Equal(8080, value.HTTP2Port);
    }

    [Fact]
    public void Nested_groups_keep_initialized_defaults_and_use_double_underscores()
    {
        var app = TestSettings.Load<Nested>(("DATABASE__POOL_SIZE", "7"), ("CACHE__CACHE_TTL", "9"));
        Assert.Equal(7, app.Database.PoolSize);
        Assert.Equal(77, app.Database.Port);
        Assert.Equal(9, app.Cache.Ttl);
    }

    [Fact]
    public void Optional_nested_group_is_created_only_for_a_known_supplied_key()
    {
        Assert.Null(TestSettings.Load<OptionalNested>(("DATABASE__UNKNOWN", "1")).Database);
        Assert.NotNull(TestSettings.Load<OptionalNested>(("DATABASE__PORT", "42")).Database);
    }

    [Fact]
    public void A_base_class_prefix_is_not_inherited_by_a_derived_group()
        => Assert.Equal(17, TestSettings.Load<DerivedPrefix>(("VALUE", "17")).Value);

    [Fact]
    public void Root_prefix_and_nested_prefix_compose_but_an_alias_bypasses_both()
    {
        var value = TestSettings.Load<PrefixedRoot>(("ROOT_CACHE__CACHE_TTL", "8"), ("EXACT_PRIVATE_KEY", "private"));
        Assert.Equal(8, value.Cache.Ttl);
        Assert.Equal("private", value.Vapid.PrivateKey);
    }

    [Fact]
    public void Missing_and_explicit_empty_values_are_distinct()
    {
        Assert.Equal(3, TestSettings.Load<NullableValues>().Number);
        var value = TestSettings.Load<NullableValues>(("NUMBER", ""), ("TEXT", ""));
        Assert.Null(value.Number);
        Assert.Equal("", value.Text);
    }

    [Fact]
    public void Keys_are_case_sensitive_and_unknown_keys_are_ignored()
        => Assert.Equal(8000, TestSettings.Load<App>(("app_port", "9000"), ("UNKNOWN", "anything")).Port);

    [Theory]
    [InlineData(typeof(Cycle))]
    [InlineData(typeof(ListModel))]
    [InlineData(typeof(NoDefaultConstructor))]
    [InlineData(typeof(DuplicateKey))]
    [InlineData(typeof(ReadOnlyAnnotated))]
    [InlineData(typeof(ComplexAlias))]
    public void Invalid_model_shapes_fail_even_without_env_values(Type type)
    {
        var method = typeof(BindingTests).GetMethod(nameof(LoadInvalid), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(type);
        var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, null));
        var settingsError = Assert.IsType<SettingsException>(error.InnerException);
        Assert.All(settingsError.Errors, e => Assert.Equal(SettingsErrorCode.Model, e.Code));
    }

    private static void LoadInvalid<T>() where T : class => TestSettings.Load<T>();

    [Fact]
    public void Constructor_getter_and_setter_exceptions_do_not_expose_user_messages()
    {
        var errors = new[]
        {
            Assert.Throws<SettingsException>(() => TestSettings.Load<BadConstructor>()),
            Assert.Throws<SettingsException>(() => TestSettings.Load<BadGetter>()),
            Assert.Throws<SettingsException>(() => TestSettings.Load<BadSetter>(("VALUE", "secret-marker")))
        };
        Assert.All(errors, e => { Assert.DoesNotContain("secret-marker", e.ToString()); Assert.Null(e.InnerException); });
    }

    public abstract class DatabaseBase { public int DbPoolSize { get; init; } = 3; }
    public sealed class App : DatabaseBase
    {
        public string ProjectName { get; init; } = "Widget iSpace";
        [Env("APP_PORT")] public int Port { get; init; } = 8000;
    }
    [EnvPrefix("VAPID_")]
    public sealed class Vapid
    {
        public string PublicKey { get; init; } = "";
        [Env("EXACT_PRIVATE_KEY"), Secret] public string PrivateKey { get; init; } = "";
        public string Computed => "computed";
        public Dictionary<string, string> GetKeys() => new() { ["publicKey"] = PublicKey };
    }
    public sealed class Names
    {
        public string APIPrefix { get; init; } = "";
        public int DB_POOL_SIZE { get; init; }
        public int HTTP2Port { get; init; }
    }
    public sealed class Database { public int PoolSize { get; init; } = 3; public int Port { get; init; } = 5432; }
    [EnvPrefix("CACHE_")] public sealed class Cache { public int Ttl { get; init; } }
    public sealed class Nested
    {
        public Database Database { get; init; } = new() { Port = 77 };
        public Cache Cache { get; init; } = new();
    }
    public sealed class OptionalNested { public Database? Database { get; init; } }
    [EnvPrefix("ROOT_")] public sealed class PrefixedRoot
    {
        public Cache Cache { get; init; } = new();
        public Vapid Vapid { get; init; } = new();
    }
    [EnvPrefix("BASE_")] public class BasePrefix { public int Value { get; init; } }
    public sealed class DerivedPrefix : BasePrefix;
    public sealed class NullableValues { public int? Number { get; init; } = 3; public string? Text { get; init; } = "default"; }
    public sealed class Cycle { public Cycle? Next { get; init; } }
    public sealed class ListModel { public List<string> Values { get; init; } = []; }
    public sealed class NoDefaultConstructor(int value) { public int Value { get; init; } = value; }
    public sealed class DuplicateKey { [Env("SAME")] public int First { get; init; } [Env("SAME")] public int Second { get; init; } }
    public sealed class ReadOnlyAnnotated { [Required] public string Value => "x"; }
    public sealed class ComplexAlias { [Env("DB")] public Database Database { get; init; } = new(); }
    public sealed class BadConstructor { public BadConstructor() => throw new Exception("secret-marker"); }
    public sealed class BadGetter { public string Value { get => throw new Exception("secret-marker"); init { } } }
    public sealed class BadSetter { public string Value { get => ""; init => throw new Exception(value); } }
}
