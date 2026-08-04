using ERechnung.Core.Models;

namespace ERechnung.Tests.Unit;

public sealed class RechnungsPdfVerknuepfungTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutRelativePath_ThrowsArgumentException(string relativerPfad)
    {
        Assert.Throws<ArgumentException>(
            () => new RechnungsPdfVerknuepfung(
                relativerPfad,
                DateTime.UtcNow,
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNonUtcCreationTime_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new RechnungsPdfVerknuepfung(
                "rechnungen/42/v1.pdf",
                new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Local),
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithValidData_PreservesInvoiceVersion()
    {
        var erstelltAm = new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc);
        var rechnungsstandAm = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc);

        var verknuepfung = new RechnungsPdfVerknuepfung(
            "rechnungen/42/v1.pdf",
            erstelltAm,
            rechnungsstandAm);

        Assert.Equal("rechnungen/42/v1.pdf", verknuepfung.RelativerPfad);
        Assert.Equal(erstelltAm, verknuepfung.ErstelltAm);
        Assert.Equal(rechnungsstandAm, verknuepfung.RechnungsstandAm);
    }
}
