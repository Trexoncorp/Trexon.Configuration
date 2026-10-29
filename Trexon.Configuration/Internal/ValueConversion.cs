using System.Globalization;

namespace Trexon.Configuration.Internal;

internal static class ValueConversion
{
    internal static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum || type == typeof(string) || type == typeof(bool)
            || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float) || type == typeof(double) || type == typeof(decimal)
            || type == typeof(Guid) || type == typeof(Uri) || type == typeof(TimeSpan)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly) || type == typeof(TimeOnly);
    }

    internal static object? Convert(string text, Type target)
    {
        var underlying = Nullable.GetUnderlyingType(target);
        if (underlying != null && text.Length == 0) return null;
        var type = underlying ?? target;
        if (type == typeof(string)) return text;
        if (type == typeof(bool)) return text.Trim().ToLowerInvariant() switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => throw new FormatException()
        };
        if (type.IsEnum)
        {
            // Names only: numeric enum values and flag combinations are deliberately excluded.
            var name = Enum.GetNames(type).FirstOrDefault(n => string.Equals(n, text.Trim(), StringComparison.OrdinalIgnoreCase));
            return name == null ? throw new FormatException() : Enum.Parse(type, name);
        }
        var culture = CultureInfo.InvariantCulture;
        if (type == typeof(Guid)) return Guid.Parse(text);
        if (type == typeof(Uri))
            return Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.IsWellFormedOriginalString()
                ? uri : throw new FormatException();
        if (type == typeof(TimeSpan)) return TimeSpan.ParseExact(text, "c", culture);
        if (type == typeof(DateTime)) return DateTime.ParseExact(text, "O", culture, DateTimeStyles.RoundtripKind);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.ParseExact(text, "O", culture, DateTimeStyles.None);
        if (type == typeof(DateOnly)) return DateOnly.ParseExact(text, "yyyy-MM-dd", culture);
        if (type == typeof(TimeOnly)) return TimeOnly.ParseExact(text, ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], culture);
        if (type == typeof(float))
        {
            var value = float.Parse(text, NumberStyles.Float, culture);
            return float.IsFinite(value) ? value : throw new FormatException();
        }
        if (type == typeof(double))
        {
            var value = double.Parse(text, NumberStyles.Float, culture);
            return double.IsFinite(value) ? value : throw new FormatException();
        }
        if (type == typeof(decimal)) return decimal.Parse(text, NumberStyles.Float, culture);
        // Integer conversions are checked for overflow and do not accept grouping separators.
        return System.Convert.ChangeType(text, type, culture);
    }
}
