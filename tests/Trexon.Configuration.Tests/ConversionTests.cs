using System.Globalization;

namespace Trexon.Configuration.Tests;

public sealed class ConversionTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    public void Boolean_forms_are_explicit(string text, bool expected)
        => Assert.Equal(expected, TestSettings.Load<Value<bool>>(("ITEM", text)).Item);

    [Fact]
    public void All_numeric_types_use_invariant_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.Equal((byte)255, Read<byte>("255"));
            Assert.Equal((sbyte)-128, Read<sbyte>("-128"));
            Assert.Equal((short)-32768, Read<short>("-32768"));
            Assert.Equal((ushort)65535, Read<ushort>("65535"));
            Assert.Equal(int.MinValue, Read<int>("-2147483648"));
            Assert.Equal(uint.MaxValue, Read<uint>("4294967295"));
            Assert.Equal(long.MinValue, Read<long>("-9223372036854775808"));
            Assert.Equal(ulong.MaxValue, Read<ulong>("18446744073709551615"));
            Assert.Equal(0.2, Read<double>("0.2"));
            Assert.Equal(0.2f, Read<float>("0.2"));
            Assert.Equal(100m, Read<decimal>("1e2"));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Structured_scalar_formats_round_trip()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, Read<Guid>(id.ToString()));
        Assert.Equal("https://example.com/", Read<Uri>("https://example.com").AbsoluteUri);
        Assert.Equal(TimeSpan.FromMilliseconds(200), Read<TimeSpan>("00:00:00.2000000"));
        Assert.Equal(new DateOnly(2026, 10, 29), Read<DateOnly>("2026-10-29"));
        Assert.Equal(new TimeOnly(12, 30, 1), Read<TimeOnly>("12:30:01"));
        var now = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, Read<DateTime>(now.ToString("O")));
        var offset = new DateTimeOffset(now);
        Assert.Equal(offset, Read<DateTimeOffset>(offset.ToString("O")));
        Assert.Equal(Mode.Production, Read<Mode>("production"));
        Assert.Null(Read<Guid?>(""));
    }

    [Theory]
    [InlineData(typeof(int), "")]
    [InlineData(typeof(int), "hello")]
    [InlineData(typeof(int), "2147483648")]
    [InlineData(typeof(uint), "-1")]
    [InlineData(typeof(double), "0,2")]
    [InlineData(typeof(double), "NaN")]
    [InlineData(typeof(double), "Infinity")]
    [InlineData(typeof(double), "1e999")]
    [InlineData(typeof(float), "1e999")]
    [InlineData(typeof(decimal), "1,000")]
    [InlineData(typeof(bool), "yes")]
    [InlineData(typeof(Guid), "secret-marker")]
    [InlineData(typeof(Uri), "/relative")]
    [InlineData(typeof(TimeSpan), "200ms")]
    [InlineData(typeof(DateOnly), "29.10.2026")]
    [InlineData(typeof(Mode), "1")]
    [InlineData(typeof(Mode), "Development, Production")]
    public void Invalid_values_fail_without_echoing_the_input(Type type, string text)
    {
        var method = typeof(ConversionTests).GetMethod(nameof(ReadFailure), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(type);
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, [text]));
        var failure = Assert.IsType<SettingsException>(exception.InnerException);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(SettingsErrorCode.Conversion, error.Code);
        Assert.Equal("ITEM", error.Key);
        Assert.Equal("memory", error.Source);
        Assert.Null(failure.InnerException);
        Assert.DoesNotContain("secret-marker", failure.ToString());
    }

    private static void ReadFailure<T>(string text) => Read<T>(text);
    private static T Read<T>(string text) => TestSettings.Load<Value<T>>(("ITEM", text)).Item;
    public sealed class Value<T> { public T Item { get; init; } = default!; }
    public enum Mode { Development, Production }
}
