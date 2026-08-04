using ERechnung.Core.Models;
using ERechnung.Core.Services;
using ERechnung.XML.Generators;

var invoice = new Rechnung
{
    Nummer = "RE-2026-0042",
    Rechnungsdatum = new DateTime(2026, 8, 3),
    Waehrung = "EUR",
    Bemerkung = "Test",
    EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
    {
        QuellId = 11,
        Name = "Test Kunde GmbH",
        Strasse = "Teststr. 1",
        PLZ = "10115",
        Ort = "Berlin",
        Land = "DE",
        Email = "kunde@test.com",
        UstIdNr = "DE123456789"
    },
    AbsenderSnapshot = new RechnungsAbsenderSnapshot
    {
        QuellId = 21,
        Name = "Test Absender GmbH",
        Strasse = "Absenderweg 5",
        PLZ = "10117",
        Ort = "Berlin",
        Land = "DE",
        Email = "absender@test.com",
        Telefon = "+49 30 123",
        UstIdNr = "DE987654321",
        IBAN = "DE12500105170648489890",
        BIC = "INGDDEFFXXX"
    },
    Positionen =
    [
        new RechnungsPosition
        {
            Reihenfolge = 1,
            Menge = 2m,
            EinzelpreisNetto = 100m,
            Steuersatz = 19m,
            Beschreibung = "Testdienstleistung",
            Einheit = "ST"
        }
    ]
};

RechnungCalculator.Berechnen(invoice);

var gen = new UblGenerator();
var xml = gen.Generate(invoice);
Console.WriteLine(xml);
