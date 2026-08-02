using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class KundeValidatorTests
{
    [Fact]
    public void Validate_WithValidCustomer_ReturnsNoErrors()
    {
        var kunde = new Kunde
        {
            Firmenname = new string('A', 200),
            Email = "rechnung@example.com",
            Land = "DE",
            PLZ = new string('1', 20)
        };

        var errors = KundeValidator.Validate(kunde);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutCompanyName_ReturnsRequiredError(string firmenname)
    {
        var kunde = CreateValidCustomer();
        kunde.Firmenname = firmenname;

        var errors = KundeValidator.Validate(kunde);

        Assert.Contains("Der Firmenname ist erforderlich.", errors);
    }

    [Fact]
    public void Validate_WithTooLongCompanyName_ReturnsLengthError()
    {
        var kunde = CreateValidCustomer();
        kunde.Firmenname = new string('A', 201);

        var errors = KundeValidator.Validate(kunde);

        Assert.Contains("Der Firmenname darf höchstens 200 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsEmailError()
    {
        var kunde = CreateValidCustomer();
        kunde.Email = "ungueltig@@example.com";

        var errors = KundeValidator.Validate(kunde);

        Assert.Contains("Die E-Mail-Adresse ist ungültig.", errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("DEU")]
    public void Validate_WithInvalidCountryCode_ReturnsCountryError(string land)
    {
        var kunde = CreateValidCustomer();
        kunde.Land = land;

        var errors = KundeValidator.Validate(kunde);

        Assert.Contains("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.", errors);
    }

    [Fact]
    public void Validate_WithTooLongPostalCode_ReturnsLengthError()
    {
        var kunde = CreateValidCustomer();
        kunde.PLZ = new string('1', 21);

        var errors = KundeValidator.Validate(kunde);

        Assert.Contains("Die Postleitzahl darf höchstens 20 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithSeveralInvalidFields_ReturnsEveryError()
    {
        var kunde = new Kunde
        {
            Firmenname = " ",
            Email = "ungueltig@@example.com",
            Land = "DEU",
            PLZ = new string('1', 21)
        };

        var errors = KundeValidator.Validate(kunde);

        Assert.Equal(4, errors.Count);
        Assert.Contains("Der Firmenname ist erforderlich.", errors);
        Assert.Contains("Die E-Mail-Adresse ist ungültig.", errors);
        Assert.Contains("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.", errors);
        Assert.Contains("Die Postleitzahl darf höchstens 20 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithNullCustomer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => KundeValidator.Validate(null!));
    }

    private static Kunde CreateValidCustomer()
    {
        return new Kunde
        {
            Firmenname = "Muster GmbH",
            Email = "rechnung@example.com",
            Land = "DE",
            PLZ = "12345"
        };
    }
}
