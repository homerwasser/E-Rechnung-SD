using System.Globalization;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using ERechnung.XML.Generators;
using ERechnung.XML.Parsers;

namespace ERechnung.Tests.Unit;

/// <summary>
/// Tests whether the UblParser correctly reconstructs a Rechnung from UBL 2.2 XML
/// produced by the UblGenerator.
/// </summary>
public sealed class UblParserTests
{
    private readonly IUblGenerator _generator = new UblGenerator();
    private readonly IUblParser _parser = new UblParser();

    [Fact]
    public void Parse_RoundTrip_PreservesInvoiceNumber()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Nummer, parsed.Nummer);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesIssueDate()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Rechnungsdatum.Date, parsed.Rechnungsdatum.Date);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesComment()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Bemerkung, parsed.Bemerkung);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesBuyerName()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.NotNull(parsed.EmpfaengerSnapshot);
        Assert.Equal(rechnung.EmpfaengerSnapshot!.Name, parsed.EmpfaengerSnapshot.Name);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesSellerName()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.NotNull(parsed.AbsenderSnapshot);
        Assert.Equal(rechnung.AbsenderSnapshot!.Name, parsed.AbsenderSnapshot.Name);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesCurrency()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Waehrung, parsed.Waehrung);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesNetAmount()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.GesamtbetragNetto, parsed.GesamtbetragNetto);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesTaxAmount()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.UmsatzsteuerBetrag, parsed.UmsatzsteuerBetrag);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesGrossAmount()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.GesamtbetragBrutto, parsed.GesamtbetragBrutto);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesPerformanceDate()
    {
        var rechnung = CreateInvoice();
        rechnung.Leistungsdatum = new DateTime(2026, 7, 15);
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Leistungsdatum!.Value.Date, parsed.Leistungsdatum!.Value.Date);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesDueDate()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Faeligkeitsdatum!.Value.Date, parsed.Faeligkeitsdatum!.Value.Date);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemCount()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Positionen.Count, parsed.Positionen.Count);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemDescription()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(
            rechnung.Positionen[0].Beschreibung,
            parsed.Positionen[0].Beschreibung);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemQuantity()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Positionen[0].Menge, parsed.Positionen[0].Menge);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemUnitPrice()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Positionen[0].EinzelpreisNetto, parsed.Positionen[0].EinzelpreisNetto);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemTaxRate()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Positionen[0].Steuersatz, parsed.Positionen[0].Steuersatz);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesLineItemUnit()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.Positionen[0].Einheit, parsed.Positionen[0].Einheit);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesVatIds()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(rechnung.EmpfaengerSnapshot!.UstIdNr, parsed.EmpfaengerSnapshot!.UstIdNr);
        Assert.Equal(rechnung.AbsenderSnapshot!.UstIdNr, parsed.AbsenderSnapshot!.UstIdNr);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesSellerContactInfo()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        var absender = parsed.AbsenderSnapshot!;
        Assert.Equal(rechnung.AbsenderSnapshot!.Ansprechpartner, absender.Ansprechpartner);
        Assert.Equal(rechnung.AbsenderSnapshot.Telefon, absender.Telefon);
        Assert.Equal(rechnung.AbsenderSnapshot.Email, absender.Email);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesBuyerContactInfo()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        var empfaenger = parsed.EmpfaengerSnapshot!;
        Assert.Equal(rechnung.EmpfaengerSnapshot!.Ansprechpartner, empfaenger.Ansprechpartner);
        Assert.Equal(rechnung.EmpfaengerSnapshot.Email, empfaenger.Email);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesPostalAddress()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        var empfaenger = parsed.EmpfaengerSnapshot!;
        Assert.Equal(rechnung.EmpfaengerSnapshot!.Strasse, empfaenger.Strasse);
        Assert.Equal(rechnung.EmpfaengerSnapshot.PLZ, empfaenger.PLZ);
        Assert.Equal(rechnung.EmpfaengerSnapshot.Ort, empfaenger.Ort);
        Assert.Equal(rechnung.EmpfaengerSnapshot.Land, empfaenger.Land);
    }

    [Fact]
    public void Parse_RoundTrip_ProducesCorrectComputedValues()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        // GesamtpreisNetto is computed from Menge * EinzelpreisNetto
        Assert.Equal(
            rechnung.Positionen[0].GesamtpreisNetto,
            parsed.Positionen[0].GesamtpreisNetto);
    }

    [Fact]
    public void Parse_EmptyXml_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_NullXml_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_InvalidXml_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _parser.Parse("<not valid xml"));
    }

    [Fact]
    public void Parse_WrongRootElement_ThrowsInvalidOperationException()
    {
        var xml = @"<?xml version=""1.0""?><Invoice><Number>123</Number></Invoice>";
        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(xml));
        Assert.Contains("CrossIndustryInvoice", ex.Message);
    }

    [Fact]
    public void Parse_MissingExchangedDocument_ThrowsInvalidOperationException()
    {
        // Wohlgeformtes XML mit leerem CrossIndustryInvoice – kein ExchangedDocument vorhanden
        var xml = "<?xml version=\"1.0\"?><rsm:CrossIndustryInvoice xmlns:rsm=\"urn:oasis:names:specification:ubl:schema:xsd:CrossIndustryInvoice-1\"></rsm:CrossIndustryInvoice>";
        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(xml));
        Assert.Contains("ExchangedDocument", ex.Message);
    }

    [Fact]
    public void Parse_MissingSupplyChainTradeTransaction_ThrowsInvalidOperationException()
    {
        var xml = @"<?xml version=""1.0""?><CrossIndustryInvoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:CrossIndustryInvoice-1""><ExchangedDocument/><Missing/></CrossIndustryInvoice>";
        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(xml));
        Assert.Contains("SupplyChainTradeTransaction", ex.Message);
    }

    [Fact]
    public void Parse_MultipleLineItems_ParsesAllPositions()
    {
        var rechnung = CreateInvoice();
        rechnung.Positionen.Add(new RechnungsPosition
        {
            Reihenfolge = 2,
            Beschreibung = "Zweite Position",
            Menge = 3m,
            EinzelpreisNetto = 50m,
            Steuersatz = 7m,
            Einheit = "ST",
        });
        RechnungCalculator.Berechnen(rechnung);

        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        Assert.Equal(2, parsed.Positionen.Count);
        Assert.Equal("Zweite Position", parsed.Positionen[1].Beschreibung);
        Assert.Equal(3m, parsed.Positionen[1].Menge);
        Assert.Equal(50m, parsed.Positionen[1].EinzelpreisNetto);
        Assert.Equal(7m, parsed.Positionen[1].Steuersatz);
    }

    [Fact]
    public void Parse_FullRoundTrip_ProducesValidInvoiceForSaving()
    {
        var rechnung = CreateInvoice();
        var xml = _generator.Generate(rechnung);
        var parsed = _parser.Parse(xml);

        // The parsed invoice should have all required fields for saving
        Assert.False(string.IsNullOrEmpty(parsed.Nummer));
        Assert.NotNull(parsed.EmpfaengerSnapshot);
        Assert.NotNull(parsed.AbsenderSnapshot);
        Assert.NotEmpty(parsed.Positionen);

        // When saved, the service will assign Id,timestamps, etc.
        Assert.Null(parsed.Id);
    }

    private static Rechnung CreateInvoice()
    {
        var invoice = new Rechnung
        {
            Nummer = "RE-2026-0042",
            Titel = "Synthetische Testrechnung",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            Erstellungsdatum = new DateTime(2026, 8, 3),
            Faeligkeitsdatum = new DateTime(2026, 8, 17),
            Waehrung = "EUR",
            Bemerkung = "Synthetische Testrechnung",
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = 11,
                Name = "Synthetischer Kunde GmbH",
                Ansprechpartner = "Max Mustermann",
                Strasse = "Musterstraße 1",
                PLZ = "10115",
                Ort = "Berlin",
                Land = "DE",
                Email = "kunde@example.com",
                UstIdNr = "DE123456789",
            },
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = 21,
                Name = "Synthetischer Absender GmbH",
                Ansprechpartner = "Firma Müller",
                Strasse = "Lieferantenallee 5",
                PLZ = "10117",
                Ort = "Berlin",
                Land = "DE",
                Email = "absender@example.com",
                Telefon = "+49 30 123456",
                UstIdNr = "DE987654321",
                IBAN = "DE12500105170648489890",
                BIC = "INGDDEFFXXX",
            },
            Positionen = [CreatePosition(2m, 100m, 19m, 1)],
        };
        RechnungCalculator.Berechnen(invoice);
        return invoice;
    }

    private static RechnungsPosition CreatePosition(
        decimal menge, decimal einzelpreisNetto, decimal steuersatz, int reihenfolge)
    {
        return new RechnungsPosition
        {
            Reihenfolge = reihenfolge,
            Menge = menge,
            EinzelpreisNetto = einzelpreisNetto,
            Steuersatz = steuersatz,
            Beschreibung = "Synthetische Dienstleistung",
            Einheit = "ST",
        };
    }
}