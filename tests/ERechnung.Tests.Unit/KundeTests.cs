using ERechnung.Core.Models;

namespace ERechnung.Tests.Unit;

public sealed class KundeTests
{
    [Fact]
    public void Constructor_SetsSafeDefaults()
    {
        var beforeCreation = DateTime.UtcNow;

        var kunde = new Kunde();

        var afterCreation = DateTime.UtcNow;
        Assert.Null(kunde.Id);
        Assert.Equal(string.Empty, kunde.Firmenname);
        Assert.Equal(string.Empty, kunde.Ansprechpartner);
        Assert.Equal(string.Empty, kunde.Strasse);
        Assert.Equal(string.Empty, kunde.PLZ);
        Assert.Equal(string.Empty, kunde.Ort);
        Assert.Equal("DE", kunde.Land);
        Assert.Equal(string.Empty, kunde.Email);
        Assert.Equal(string.Empty, kunde.Telefon);
        Assert.Null(kunde.UstIdNr);
        Assert.Equal(string.Empty, kunde.Bemerkung);
        Assert.InRange(kunde.ErstelltAm, beforeCreation, afterCreation);
    }

    [Fact]
    public void Properties_RetainCompleteCustomerData()
    {
        var erstelltAm = new DateTime(2026, 7, 15, 10, 30, 0, DateTimeKind.Utc);

        var kunde = new Kunde
        {
            Id = 42,
            Firmenname = "Muster GmbH",
            Ansprechpartner = "Erika Muster",
            Strasse = "Musterweg 7",
            PLZ = "12345",
            Ort = "Musterstadt",
            Land = "DE",
            Email = "erika@muster.example",
            Telefon = "+49 30 123456",
            UstIdNr = "DE123456789",
            Bemerkung = "Bevorzugt E-Rechnungen",
            ErstelltAm = erstelltAm
        };

        Assert.Equal(42, kunde.Id);
        Assert.Equal("Muster GmbH", kunde.Firmenname);
        Assert.Equal("Erika Muster", kunde.Ansprechpartner);
        Assert.Equal("Musterweg 7", kunde.Strasse);
        Assert.Equal("12345", kunde.PLZ);
        Assert.Equal("Musterstadt", kunde.Ort);
        Assert.Equal("DE", kunde.Land);
        Assert.Equal("erika@muster.example", kunde.Email);
        Assert.Equal("+49 30 123456", kunde.Telefon);
        Assert.Equal("DE123456789", kunde.UstIdNr);
        Assert.Equal("Bevorzugt E-Rechnungen", kunde.Bemerkung);
        Assert.Equal(erstelltAm, kunde.ErstelltAm);
    }
}
