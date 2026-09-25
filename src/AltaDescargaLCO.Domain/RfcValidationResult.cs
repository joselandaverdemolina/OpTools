namespace AltaDescargaLCO.Domain;

public sealed record RfcValidationResult(bool IsValid, string? ErrorMessage, PersonaType? Type)
{
    public static RfcValidationResult Valid(PersonaType type) => new(true, null, type);

    public static RfcValidationResult Invalid(string errorMessage) => new(false, errorMessage, null);
}
