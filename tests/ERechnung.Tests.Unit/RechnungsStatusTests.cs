using ERechnung.Core.Models;

namespace ERechnung.Tests.Unit;

public sealed class RechnungsStatusTests
{
    [Fact]
    public void Alle_ContainsExactlyPersistentValuesInDefinedOrder()
    {
        Assert.Equal(
            [
                "erstellt",
                "versendet",
                "offen",
                "bezahlt",
                "inklaerung",
                "storniert"
            ],
            RechnungsStatus.Alle);
    }

    [Fact]
    public void Alle_CannotBeChangedThroughListInterface()
    {
        var list = Assert.IsAssignableFrom<IList<string>>(RechnungsStatus.Alle);

        Assert.Throws<NotSupportedException>(() => list.Add("neu"));
    }

    [Theory]
    [InlineData("erstellt", true)]
    [InlineData("versendet", true)]
    [InlineData("offen", true)]
    [InlineData("bezahlt", true)]
    [InlineData("inklaerung", true)]
    [InlineData("storniert", true)]
    [InlineData("Erstellt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_RecognizesOnlyCanonicalValues(string? status, bool expected)
    {
        Assert.Equal(expected, RechnungsStatus.IsValid(status));
    }

    [Fact]
    public void Anzeigenamen_ProvideGermanNames()
    {
        Assert.Equal("Erstellt", RechnungsStatus.GetAnzeigename(RechnungsStatus.Erstellt));
        Assert.Equal("In Klärung", RechnungsStatus.Anzeigenamen[RechnungsStatus.Inklaerung]);
        Assert.Equal("Storniert", RechnungsStatus.GetAnzeigename(RechnungsStatus.Storniert));
    }

    [Fact]
    public void Rechnung_UsesCanonicalDefaults()
    {
        var rechnung = new Rechnung();

        Assert.Equal(RechnungsStatus.Erstellt, rechnung.Status);
        Assert.Equal("EUR", rechnung.Waehrung);
    }
}
