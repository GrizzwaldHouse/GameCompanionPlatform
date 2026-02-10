namespace GameCompanion.Core.Models;

/// <summary>
/// Result of validating a save field value.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Valid() => new(true, null);
    public static ValidationResult Invalid(string errorMessage) => new(false, errorMessage);

    public static implicit operator bool(ValidationResult result) => result.IsValid;
}
