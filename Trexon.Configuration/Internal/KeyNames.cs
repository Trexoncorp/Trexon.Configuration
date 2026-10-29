using System.Text;

namespace Trexon.Configuration.Internal;

internal sealed record RawValue(string Text, string Source);

internal static class KeyNames
{
    internal static bool IsValid(string name) => name.Length > 0
        && (char.IsAsciiLetter(name[0]) || name[0] == '_')
        && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    internal static void Validate(string name, string parameter)
    {
        ArgumentNullException.ThrowIfNull(name, parameter);
        if (!IsValid(name)) throw new ArgumentException("Expected an ASCII environment key: [A-Za-z_][A-Za-z0-9_]*.", parameter);
    }

    internal static string FromProperty(string name)
    {
        var result = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && name[i - 1] != '_'
                && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])
                    || (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1]))))
                result.Append('_');
            result.Append(char.ToUpperInvariant(c));
        }
        return result.ToString();
    }
}
