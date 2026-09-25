using System.Text.RegularExpressions;

namespace AltaDescargaLCO.Domain;

public static class RfcValidator
{
     private static readonly Regex MoralPattern =
        new(@"^[A-ZÑ&]{3}[0-9]{6}[A-Z0-9]{2}[0-9A]$", RegexOptions.CultureInvariant);

    private static readonly Regex FisicaPattern =
        new(@"^[A-ZÑ&]{4}[0-9]{6}[A-Z0-9]{2}[0-9A]$", RegexOptions.CultureInvariant);

     private static readonly string[] GenericRfcs = ["XAXX010101000", "XEXX010101000"];

    public static string Normalize(string raw) => raw.Trim().ToUpperInvariant();

    public static RfcValidationResult Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return RfcValidationResult.Invalid("El RFC es obligatorio.");
        }

        var rfc = Normalize(raw);

        if (GenericRfcs.Contains(rfc))
        {
            return RfcValidationResult.Invalid(
                $"'{rfc}' es un RFC genérico del SAT y no se puede registrar. Capture el RFC del PAC.");
        }

        PersonaType type;
        if (MoralPattern.IsMatch(rfc))
        {
            type = PersonaType.Moral;
        }
        else if (FisicaPattern.IsMatch(rfc))
        {
            type = PersonaType.Fisica;
        }
        else
        {
            return RfcValidationResult.Invalid(
                $"'{rfc}' no tiene la estructura de un RFC. Se esperan 12 caracteres para persona moral " +
                "o 13 para persona física: letras del nombre, después AAMMDD y una homoclave de 3 caracteres.");
        }

        var datePart = type == PersonaType.Moral ? rfc.Substring(3, 6) : rfc.Substring(4, 6);
        if (!IsValidDate(datePart))
        {
            return RfcValidationResult.Invalid(
                $"La fecha '{datePart}' contenida en el RFC no es una fecha válida con formato AAMMDD.");
        }

        if (rfc[^1] != ComputeCheckDigit(rfc[..^1]))
        {
            return RfcValidationResult.Invalid(
                $"El dígito verificador de '{rfc}' es incorrecto. Verifique que el RFC esté bien capturado.");
        }

        return RfcValidationResult.Valid(type);
    }

        public static char ComputeCheckDigit(string rfcWithoutCheckDigit)
    {
         var basis = rfcWithoutCheckDigit.Length == 11 ? " " + rfcWithoutCheckDigit : rfcWithoutCheckDigit;
        if (basis.Length != 12)
        {
            throw new ArgumentException(
                "Un RFC sin dígito verificador debe tener 11 o 12 caracteres.", nameof(rfcWithoutCheckDigit));
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var value = CharacterValue(basis[i]);
            if (value < 0)
            {
                throw new ArgumentException(
                    $"'{basis[i]}' no es un carácter permitido en un RFC.", nameof(rfcWithoutCheckDigit));
            }

            sum += value * (13 - i);
        }

        var remainder = sum % 11;
        return remainder switch
        {
            0 => '0',
            1 => 'A',
            _ => (char)('0' + 11 - remainder)
        };
    }

    private static int CharacterValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'N' => c - 'A' + 10,
        '&' => 24,
        >= 'O' and <= 'Z' => c - 'O' + 25,
        ' ' => 37,
        'Ñ' => 38,
        _ => -1
    };

    private static bool IsValidDate(string yymmdd)
    {
        var year = int.Parse(yymmdd[..2]);
        var month = int.Parse(yymmdd[2..4]);
        var day = int.Parse(yymmdd[4..]);

        if (month is < 1 or > 12 || day < 1)
        {
            return false;
        }

         return day <= DateTime.DaysInMonth(1900 + year, month)
            || day <= DateTime.DaysInMonth(2000 + year, month);
    }
}
