namespace Control.Api.Application.Validation;

public record FileValidationResult(bool IsValid, string? ErrorMessage)
{
    public static FileValidationResult Valid() => new(true, null);
    public static FileValidationResult Invalid(string message) => new(false, message);
}
