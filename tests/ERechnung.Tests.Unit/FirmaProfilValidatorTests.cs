using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class FirmaProfilValidatorTests
{
    [Fact]
    public void Validate_WithValidCompanyProfile_ReturnsNoErrors()
    {
        var firmaProfil = new FirmaProfil
        {
            Name = new string('A', 200),
            Email = "rechnung@example.com",
            Land = "DE",
            PLZ = new string('1', 20)
        };

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutName_ReturnsRequiredError(string name)
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.Name = name;

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Contains("Der Firmenname ist erforderlich.", errors);
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsLengthError()
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.Name = new string('A', 201);

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Contains("Der Firmenname darf höchstens 200 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsEmailError()
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.Email = "ungueltig@@example.com";

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Contains("Die E-Mail-Adresse ist ungültig.", errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("DEU")]
    [InlineData("12")]
    [InlineData("!?")]
    public void Validate_WithInvalidCountryCode_ReturnsCountryError(string land)
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.Land = land;

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Contains("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.", errors);
    }

    [Fact]
    public void Validate_WithLowercaseCountryCode_ReturnsNoCountryError()
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.Land = "de";

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.DoesNotContain(
            "Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.",
            errors);
    }

    [Fact]
    public void Validate_WithTooLongPostalCode_ReturnsLengthError()
    {
        var firmaProfil = CreateValidCompanyProfile();
        firmaProfil.PLZ = new string('1', 21);

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Contains("Die Postleitzahl darf höchstens 20 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithSeveralInvalidFields_ReturnsEveryError()
    {
        var firmaProfil = new FirmaProfil
        {
            Name = " ",
            Email = "ungueltig@@example.com",
            Land = "DEU",
            PLZ = new string('1', 21)
        };

        var errors = FirmaProfilValidator.Validate(firmaProfil);

        Assert.Equal(4, errors.Count);
        Assert.Contains("Der Firmenname ist erforderlich.", errors);
        Assert.Contains("Die E-Mail-Adresse ist ungültig.", errors);
        Assert.Contains("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.", errors);
        Assert.Contains("Die Postleitzahl darf höchstens 20 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithNullCompanyProfile_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FirmaProfilValidator.Validate(null!));
    }

    private static FirmaProfil CreateValidCompanyProfile()
    {
        return new FirmaProfil
        {
            Name = "Synthetische Firma GmbH",
            Email = "rechnung@example.com",
            Land = "DE",
            PLZ = "12345"
        };
    }
}
