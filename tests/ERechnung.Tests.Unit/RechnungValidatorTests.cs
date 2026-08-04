using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class RechnungValidatorTests
{
    [Fact]
    public void Validate_WithValidInvoice_ReturnsNoErrors()
    {
        var errors = RechnungValidator.Validate(CreateValidInvoice());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ForCreate_AllowsMissingInvoiceNumber()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Nummer = string.Empty;

        var errors = RechnungValidator.Validate(rechnung, rechnungsnummerErforderlich: false);

        Assert.DoesNotContain("Die Rechnungsnummer ist erforderlich.", errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ForUpdate_RequiresInvoiceNumber()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Nummer = " ";

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Die Rechnungsnummer ist erforderlich.", errors);
    }

    [Fact]
    public void Validate_WithMissingHeaderData_ReturnsEveryError()
    {
        var rechnung = new Rechnung
        {
            Nummer = string.Empty,
            Rechnungsdatum = default
        };

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Die Rechnungsnummer ist erforderlich.", errors);
        Assert.Contains("Das Rechnungsdatum ist ungültig.", errors);
        Assert.Contains("Ein Kunde ist erforderlich.", errors);
        Assert.Contains("Ein Firmenprofil ist erforderlich.", errors);
        Assert.Contains("Die Rechnung muss mindestens eine Position enthalten.", errors);
    }

    [Fact]
    public void Validate_WithInvalidPosition_ReturnsEveryPositionError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Positionen =
        [
            new RechnungsPosition
            {
                Beschreibung = " ",
                Menge = 0m,
                EinzelpreisNetto = -0.01m,
                Steuersatz = 100.01m
            }
        ];

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Position 1: Die Beschreibung ist erforderlich.", errors);
        Assert.Contains("Position 1: Die Menge muss größer als 0 sein.", errors);
        Assert.Contains("Position 1: Der Nettopreis darf nicht negativ sein.", errors);
        Assert.Contains("Position 1: Der Steuersatz muss zwischen 0 und 100 Prozent liegen.", errors);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_WithTaxRateOutsideRange_ReturnsError(double steuersatz)
    {
        var rechnung = CreateValidInvoice();
        rechnung.Positionen[0].Steuersatz = (decimal)steuersatz;

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Position 1: Der Steuersatz muss zwischen 0 und 100 Prozent liegen.", errors);
    }

    [Fact]
    public void Validate_WithDueDateBeforeInvoiceDate_ReturnsError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Faeligkeitsdatum = rechnung.Rechnungsdatum.AddDays(-1);

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Das Fälligkeitsdatum darf nicht vor dem Rechnungsdatum liegen.", errors);
    }

    [Fact]
    public void Validate_WithSameDueAndInvoiceDate_ReturnsNoDateError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Rechnungsdatum = new DateTime(2026, 8, 3, 15, 0, 0);
        rechnung.Faeligkeitsdatum = new DateTime(2026, 8, 3, 0, 0, 0);

        var errors = RechnungValidator.Validate(rechnung);

        Assert.DoesNotContain("Das Fälligkeitsdatum darf nicht vor dem Rechnungsdatum liegen.", errors);
    }

    [Fact]
    public void Validate_WithoutServiceDate_ReturnsNoServiceDateError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Leistungsdatum = null;

        var errors = RechnungValidator.Validate(rechnung);

        Assert.DoesNotContain("Das Leistungsdatum ist ungültig.", errors);
    }

    [Theory]
    [InlineData(1899, 12, 31, true)]
    [InlineData(1900, 1, 1, false)]
    [InlineData(2026, 8, 3, false)]
    public void Validate_WithServiceDate_ChecksPlausibility(
        int jahr,
        int monat,
        int tag,
        bool erwartetFehler)
    {
        var rechnung = CreateValidInvoice();
        rechnung.Leistungsdatum = new DateTime(jahr, monat, tag);

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Equal(
            erwartetFehler,
            errors.Contains("Das Leistungsdatum ist ungültig."));
    }

    [Fact]
    public void Validate_WithInvalidStatus_ReturnsError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Status = "Erstellt";

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Der Rechnungsstatus ist ungültig.", errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("€UR")]
    public void Validate_WithInvalidCurrency_ReturnsError(string waehrung)
    {
        var rechnung = CreateValidInvoice();
        rechnung.Waehrung = waehrung;

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Contains("Die Währung muss aus genau drei Buchstaben bestehen.", errors);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("usd")]
    public void Validate_WithThreeLetterCurrency_ReturnsNoCurrencyError(string waehrung)
    {
        var rechnung = CreateValidInvoice();
        rechnung.Waehrung = waehrung;

        var errors = RechnungValidator.Validate(rechnung);

        Assert.DoesNotContain("Die Währung muss aus genau drei Buchstaben bestehen.", errors);
    }

    [Fact]
    public void Validate_WithOverlongTexts_ReturnsEveryLengthError()
    {
        var rechnung = CreateValidInvoice();
        rechnung.Nummer = new string('N', 51);
        rechnung.Titel = new string('T', 201);
        rechnung.Bemerkung = new string('B', 2_001);
        rechnung.Positionen[0].Beschreibung = new string('L', 501);
        rechnung.Positionen[0].Einheit = new string('E', 21);

        var errors = RechnungValidator.Validate(rechnung);

        Assert.Equal(5, errors.Count);
        Assert.Contains("Die Rechnungsnummer darf höchstens 50 Zeichen lang sein.", errors);
        Assert.Contains("Der Rechnungstitel darf höchstens 200 Zeichen lang sein.", errors);
        Assert.Contains("Die Bemerkung darf höchstens 2000 Zeichen lang sein.", errors);
        Assert.Contains("Position 1: Die Beschreibung darf höchstens 500 Zeichen lang sein.", errors);
        Assert.Contains("Position 1: Die Einheit darf höchstens 20 Zeichen lang sein.", errors);
    }

    [Fact]
    public void Validate_WithNullInvoice_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RechnungValidator.Validate(null!));
    }

    private static Rechnung CreateValidInvoice()
    {
        return new Rechnung
        {
            Nummer = "RE-2026-0001",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            Faeligkeitsdatum = new DateTime(2026, 8, 17),
            KundeId = 1,
            FirmaProfilId = 2,
            Status = RechnungsStatus.Erstellt,
            Waehrung = "EUR",
            Positionen =
            [
                new RechnungsPosition
                {
                    Beschreibung = "Synthetische Beratungsleistung",
                    Menge = 1m,
                    EinzelpreisNetto = 100m,
                    Steuersatz = 19m
                }
            ]
        };
    }
}
