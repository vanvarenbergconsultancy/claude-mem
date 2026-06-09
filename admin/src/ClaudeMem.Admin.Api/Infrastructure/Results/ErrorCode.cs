namespace ClaudeMem.Admin.Api.Infrastructure.Results;

/// <summary>
/// A typed error code that bundles its string value, default message, and <see cref="ErrorType"/> into a single value.
/// Declared as constants on <see cref="ResultErrorCodes"/>.
/// </summary>
/// <remarks>
/// Using a dedicated struct instead of a raw <see langword="string"/> keeps the <see cref="AsError"/> factory method scoped to this type, avoiding extension-method pollution on <see langword="string"/>.
/// </remarks>
public readonly record struct ErrorCode(string Value, string DefaultMessage, ErrorType Type)
{
    /// <summary> Creates a <see cref="ResultError"/> from this code, using <paramref name="message"/> when provided or falling back to <see cref="DefaultMessage"/>. </summary>
    public ResultError AsError(string? message = null)
    {
        return new ResultError(Value, message ?? DefaultMessage, Type);
    }

    /// <summary> Allows <see cref="ErrorCode"/> to be used wherever a <see langword="string"/> is expected, such as when persisting or comparing the raw code value. </summary>
    public static implicit operator string(ErrorCode code)
    {
        return code.Value;
    }
}
