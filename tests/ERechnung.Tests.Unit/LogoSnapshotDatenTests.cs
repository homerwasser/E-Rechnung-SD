using ERechnung.Core.Models;

namespace ERechnung.Tests.Unit;

public sealed class LogoSnapshotDatenTests
{
    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    [InlineData("image/bmp")]
    public void Constructor_WithSupportedMediaType_AcceptsData(string medientyp)
    {
        var daten = new LogoSnapshotDaten([1, 2, 3], medientyp);

        Assert.Equal(medientyp, daten.Medientyp);
        Assert.Equal([1, 2, 3], daten.Inhalt);
    }

    [Fact]
    public void Constructor_WithUnsupportedMediaType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new LogoSnapshotDaten([1, 2, 3], "image/gif"));
    }

    [Fact]
    public void Inhalt_IsDefensivelyCopied()
    {
        byte[] original = [1, 2, 3];
        var daten = new LogoSnapshotDaten(original, "image/png");

        original[0] = 9;
        var ersteKopie = daten.Inhalt;
        ersteKopie[1] = 9;

        Assert.Equal([1, 2, 3], daten.Inhalt);
    }
}
