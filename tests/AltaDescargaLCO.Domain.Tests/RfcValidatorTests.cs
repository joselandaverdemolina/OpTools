using AltaDescargaLCO.Domain;

namespace AltaDescargaLCO.Domain.Tests;

public class RfcValidatorTests
{
    [Theory]
    [InlineData("GODE561231GR8", PersonaType.Fisica)]
    [InlineData("gode561231gr8", PersonaType.Fisica)]
    [InlineData("  GODE561231GR8  ", PersonaType.Fisica)]
    [InlineData("MAB0009299R7", PersonaType.Moral)]
    public void Accepts_well_formed_rfc(string raw, PersonaType expectedType)
    {
        var result = RfcValidator.Validate(raw);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(expectedType, result.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_rfc(string? raw)
    {
        var result = RfcValidator.Validate(raw);

        Assert.False(result.IsValid);
        Assert.Contains("obligatorio", result.ErrorMessage);
    }

    [Theory]
    [InlineData("GODE561231GR")]      // too short
    [InlineData("GODE561231GR88")]    // too long
    [InlineData("GOD3561231GR8")]     // digit in the name block
    [InlineData("GODE561231GRZ")]     // check digit must be 0-9 or A
    [InlineData("GODE5612A1GR8")]     // letter inside the date block
    public void Rejects_malformed_shape(string raw)
    {
        var result = RfcValidator.Validate(raw);

        Assert.False(result.IsValid);
        Assert.Contains("no tiene la estructura", result.ErrorMessage);
    }

    [Theory]
    [InlineData("GODE561331GR8")]     // month 13
    [InlineData("GODE560031GR8")]     // month 00
    [InlineData("GODE560230GR8")]     // 30 February
    [InlineData("GODE010229GR8")]     // 1901/2001 are not leap years
    public void Rejects_impossible_date(string raw)
    {
        var result = RfcValidator.Validate(raw);

        Assert.False(result.IsValid);
        Assert.Contains("no es una fecha válida", result.ErrorMessage);
    }

    [Fact]
    public void Accepts_29_february_when_the_year_can_be_a_leap_year()
    {
        // YY = 00 resolves to 2000, which is a leap year.
        var checkDigit = RfcValidator.ComputeCheckDigit("GODE000229GR");

        var result = RfcValidator.Validate($"GODE000229GR{checkDigit}");

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public void Rejects_wrong_check_digit()
    {
        var result = RfcValidator.Validate("GODE561231GR7");

        Assert.False(result.IsValid);
        Assert.Contains("dígito verificador", result.ErrorMessage);
    }

    [Theory]
    [InlineData("XAXX010101000")]
    [InlineData("XEXX010101000")]
    public void Rejects_the_sat_generic_rfcs(string raw)
    {
        var result = RfcValidator.Validate(raw);

        Assert.False(result.IsValid);
        Assert.Contains("RFC genérico del SAT", result.ErrorMessage);
    }

    [Theory]
    [InlineData("GODE561231GR", '8')]   // canonical SAT example, remainder 3
    [InlineData("MAB0009299R", '7')]    // persona moral, left-padded with a space
    public void Computes_the_documented_check_digit(string rfcWithoutCheckDigit, char expected)
    {
        Assert.Equal(expected, RfcValidator.ComputeCheckDigit(rfcWithoutCheckDigit));
    }

    [Fact]
    public void Maps_remainder_zero_to_zero_and_remainder_one_to_A()
    {
        var digits = new List<char>();
        // Sweep the last homoclave character so every remainder shows up, then assert the two
        // special cases are reachable and spelled the documented way.
        foreach (var c in "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ")
        {
            digits.Add(RfcValidator.ComputeCheckDigit($"GODE561231G{c}"));
        }

        Assert.Contains('0', digits);
        Assert.Contains('A', digits);
        Assert.DoesNotContain('B', digits);
    }

    [Fact]
    public void TryParse_returns_a_normalized_rfc()
    {
        Assert.True(Rfc.TryParse(" gode561231gr8 ", out var rfc, out var error));

        Assert.Null(error);
        Assert.Equal("GODE561231GR8", rfc!.Value);
        Assert.Equal(PersonaType.Fisica, rfc.Type);
    }

    [Fact]
    public void TryParse_reports_the_validation_error()
    {
        Assert.False(Rfc.TryParse("NOPE", out var rfc, out var error));

        Assert.Null(rfc);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}

