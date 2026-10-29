using System.Text;

namespace Trexon.Configuration.Internal;

internal static class DotEnvReader
{
    internal static IReadOnlyDictionary<string, RawValue> Read(string path, bool optional)
    {
        var source = "dotenv:" + path;
        var lineNumber = 0;
        try
        {
            var result = new Dictionary<string, RawValue>(StringComparer.Ordinal);
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
            while (reader.ReadLine() is { } rawLine)
            {
                lineNumber++;
                if (lineNumber == 1) rawLine = rawLine.TrimStart('\uFEFF');
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
                var equals = line.IndexOf('=');
                if (equals < 1) throw new FormatException();
                var key = line[..equals].Trim();
                if (!KeyNames.IsValid(key)) throw new FormatException();
                result[key] = new(ParseValue(line[(equals + 1)..].Trim()), source);
            }
            return result;
        }
        catch (FileNotFoundException) when (optional) { return new Dictionary<string, RawValue>(); }
        catch (DirectoryNotFoundException) when (optional) { return new Dictionary<string, RawValue>(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or DecoderFallbackException)
        {
            var message = exception switch
            {
                DecoderFallbackException => "Invalid UTF-8 encoding in dotenv file.",
                FormatException => $"Invalid dotenv syntax at line {lineNumber}.",
                _ => "Cannot read dotenv file."
            };
            throw new SettingsException([new("$source", null, source, SettingsErrorCode.Source, message)]);
        }
    }

    private static string ParseValue(string text)
    {
        if (text.Length == 0) return "";
        if (text[0] is not ('\'' or '"'))
        {
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '#' && (i == 0 || char.IsWhiteSpace(text[i - 1]))) return text[..i].TrimEnd();
            return text;
        }
        var quote = text[0];
        var value = new StringBuilder();
        for (var i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (c == quote)
            {
                var tail = text[(i + 1)..];
                if (tail.Length > 0 && (!char.IsWhiteSpace(tail[0]) || !tail.TrimStart().StartsWith('#')))
                    throw new FormatException();
                return value.ToString();
            }
            if (c == '\\' && quote == '"')
            {
                if (++i >= text.Length) throw new FormatException();
                c = text[i] switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"',
                    _ => throw new FormatException()
                };
            }
            value.Append(c);
        }
        throw new FormatException();
    }
}
