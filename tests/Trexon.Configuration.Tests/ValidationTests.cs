using System.ComponentModel.DataAnnotations;

namespace Trexon.Configuration.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Required_and_range_failures_are_aggregated_and_defaults_are_validated()
    {
        var failure = Assert.Throws<SettingsException>(() => TestSettings.Load<Validated>(("PORT", "70000")));
        Assert.Equal(2, failure.Errors.Count);
        Assert.All(failure.Errors, e => Assert.Equal(SettingsErrorCode.Validation, e.Code));
        Assert.Contains(failure.Errors, e => e.Key == "TOKEN" && e.IsSecret && e.Source == "default");
        Assert.Contains(failure.Errors, e => e.Key == "PORT" && e.Source == "memory" && e.Message.Contains("RangeAttribute"));
        Assert.Equal(9000, TestSettings.Load<Validated>(("PORT", "9000"), ("TOKEN", "present")).Port);
    }

    [Fact]
    public void Nested_defaults_and_required_null_groups_are_validated()
    {
        var error = Assert.Throws<SettingsException>(() => TestSettings.Load<RequiredGroup>());
        Assert.Contains(error.Errors, e => e.PropertyPath.EndsWith(".Group"));
        error = Assert.Throws<SettingsException>(() => TestSettings.Load<InitializedGroup>());
        Assert.Contains(error.Errors, e => e.PropertyPath.EndsWith(".Group.Token") && e.Key == "GROUP__TOKEN");
    }

    [Fact]
    public void Custom_validation_messages_and_exceptions_never_reach_diagnostics()
    {
        var error = Assert.Throws<SettingsException>(() => TestSettings.Load<Custom>(("TOKEN", "secret-marker")));
        Assert.DoesNotContain("secret-marker", error.ToString());
        Assert.DoesNotContain("secret-marker", string.Join(" ", error.Errors));
        Assert.Equal(2, error.Errors.Count);
    }

    [Fact]
    public void Cross_property_validation_runs_after_binding()
    {
        var failure = Assert.Throws<SettingsException>(() => TestSettings.Load<Limits>(("MIN", "20"), ("MAX", "10")));
        Assert.Contains(failure.Errors, e => e.Message.Contains("IValidatableObject"));
        Assert.DoesNotContain("secret-marker", failure.ToString());
        Assert.Equal(5, TestSettings.Load<Limits>(("MIN", "5"), ("MAX", "10")).Min);
    }

    [Fact]
    public void Class_attributes_and_nullable_required_numbers_are_validated()
    {
        Assert.Throws<SettingsException>(() => TestSettings.Load<RequiredNumber>());
        Assert.Equal(0, TestSettings.Load<RequiredNumber>(("PORT", "0")).Port);
        var failure = Assert.Throws<SettingsException>(() => TestSettings.Load<ClassValidated>());
        Assert.Equal(SettingsErrorCode.Validation, Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Property_rules_see_final_values_of_other_properties()
    {
        var settings = TestSettings.Load<Compared>(("CONFIRM", "secret-marker"), ("PASSWORD", "secret-marker"));
        Assert.Equal(settings.Password, settings.Confirm);
        var failure = Assert.Throws<SettingsException>(() => TestSettings.Load<Compared>(
            ("CONFIRM", "secret-marker"), ("PASSWORD", "different")));
        Assert.DoesNotContain("secret-marker", failure.ToString());
        Assert.Equal("normalized", TestSettings.Load<NormalizingSetter>(("TOKEN", "")).Token);
    }

    public sealed class Validated
    {
        [Range(1, 65535)] public int Port { get; init; } = 8000;
        [Required, Secret] public string Token { get; init; } = "";
    }
    public sealed class RequiredGroup { [Required] public Validated? Group { get; init; } }
    public sealed class InitializedGroup { public Validated Group { get; init; } = new(); }
    public sealed class Custom { [Leaky, Throwing, Secret] public string Token { get; init; } = ""; }
    public sealed class RequiredNumber { [Required] public int? Port { get; init; } }
    [Leaky] public sealed class ClassValidated;
    public sealed class Compared
    {
        [Secret, Compare(nameof(Password))] public string Confirm { get; init; } = "";
        [Secret] public string Password { get; init; } = "";
    }
    public sealed class NormalizingSetter
    {
        private string _token = "";
        [Required] public string Token { get => _token; init => _token = value.Length == 0 ? "normalized" : value; }
    }
    public sealed class LeakyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context) => new(value?.ToString());
    }
    public sealed class ThrowingAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => throw new Exception(value?.ToString());
    }
    public sealed class Limits : IValidatableObject
    {
        public int Min { get; init; }
        public int Max { get; init; } = 10;
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (Min > Max) yield return new("secret-marker");
        }
    }
}
