namespace Trexon.Configuration;

/// <summary>The stage at which loading failed.</summary>
public enum SettingsErrorCode
{
    /// <summary>A source could not be read or parsed.</summary>
    Source,
    /// <summary>A model has unsupported members or could not be constructed.</summary>
    Model,
    /// <summary>A supplied string could not be converted to the target type.</summary>
    Conversion,
    /// <summary>A DataAnnotations rule or object validation failed.</summary>
    Validation
}

/// <summary>A failure containing locations and rules, never setting values or user exception messages.</summary>
public sealed record SettingsError(
    string PropertyPath,
    string? Key,
    string Source,
    SettingsErrorCode Code,
    string Message,
    bool IsSecret = false);

/// <summary>Loading failed. Errors are safe to report without including raw settings values.</summary>
public sealed class SettingsException : Exception
{
    internal SettingsException(IEnumerable<SettingsError> errors) : this(errors.ToArray()) { }

    private SettingsException(SettingsError[] errors)
        : base("Settings loading failed:" + Environment.NewLine + string.Join(Environment.NewLine,
            errors.Select(e => $"{e.PropertyPath} [{e.Key ?? "-"}; {e.Source}]: {e.Message}")))
        => Errors = Array.AsReadOnly(errors);

    /// <summary>All failures collected during this load. No partially loaded model is returned.</summary>
    public IReadOnlyList<SettingsError> Errors { get; }
}
