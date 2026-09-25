namespace AltaDescargaLCO.Domain;


public sealed class Rfc
{
    private Rfc(string value, PersonaType type)
    {
        Value = value;
        Type = type;
    }

    public string Value { get; }

    public PersonaType Type { get; }

    public static bool TryParse(string? raw, out Rfc? rfc, out string? error)
    {
        var result = RfcValidator.Validate(raw);
        if (!result.IsValid)
        {
            rfc = null;
            error = result.ErrorMessage;
            return false;
        }

        rfc = new Rfc(RfcValidator.Normalize(raw!), result.Type!.Value);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}
