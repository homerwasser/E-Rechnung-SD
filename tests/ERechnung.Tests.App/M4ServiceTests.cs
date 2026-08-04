using System.IO;
using ERechnung.App.Services;
using ERechnung.Core.Models;

namespace ERechnung.Tests.App;

public sealed class M4ServiceTests
{
    public static TheoryData<byte[], string> Bildsignaturen => new()
    {
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01], "image/png" },
        { [0xFF, 0xD8, 0xFF, 0xE0, 0x01], "image/jpeg" },
        { [0x52, 0x49, 0x46, 0x46, 0x04, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50], "image/webp" },
        { [0x42, 0x4D, 0x01, 0x00], "image/bmp" }
    };

    [Theory]
    [MemberData(nameof(Bildsignaturen))]
    public void LocalLogoSnapshotLoader_ErkenntSignaturUnabhaengigVonErweiterung(
        byte[] inhalt,
        string erwarteterMedientyp)
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pfad = verzeichnis.ErstelleDatei("logo.falsch", inhalt);

        var ergebnis = new LocalLogoSnapshotLoader().Lade(pfad);

        Assert.NotNull(ergebnis);
        Assert.Equal(erwarteterMedientyp, ergebnis.Medientyp);
        Assert.Equal(inhalt, ergebnis.Inhalt);
    }

    [Fact]
    public void LocalLogoSnapshotLoader_AkzeptiertMaximalZweiMiBUndLehntMehrAb()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var maximal = new byte[LocalLogoSnapshotLoader.MaximaleDateigroesse];
        "%PDF"u8.CopyTo(maximal);
        maximal[0] = 0x42;
        maximal[1] = 0x4D;
        var zuGross = new byte[LocalLogoSnapshotLoader.MaximaleDateigroesse + 1];
        maximal.CopyTo(zuGross, 0);
        var maximalPfad = verzeichnis.ErstelleDatei("maximal.bin", maximal);
        var zuGrossPfad = verzeichnis.ErstelleDatei("zu-gross.bin", zuGross);
        var loader = new LocalLogoSnapshotLoader();

        Assert.Equal("image/bmp", loader.Lade(maximalPfad)?.Medientyp);
        Assert.Null(loader.Lade(zuGrossPfad));
    }

    [Fact]
    public void LocalLogoSnapshotLoader_GibtBeiFehlernUndUnbekanntenDatenNullZurueck()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var unbekannt = verzeichnis.ErstelleDatei("logo.png", "kein bild"u8);
        var fehlt = Path.Combine(verzeichnis.Pfad, "fehlt.png");
        var loader = new LocalLogoSnapshotLoader();

        Assert.Null(loader.Lade("relativ.png"));
        Assert.Null(loader.Lade(fehlt));
        Assert.Null(loader.Lade(verzeichnis.Pfad));
        Assert.Null(loader.Lade(unbekannt));
        Assert.Null(loader.Lade(@"\\server\freigabe\logo.png"));
        Assert.Null(loader.Lade("https://example.test/logo.png"));
    }

    [Fact]
    public void EmailEntwurfComposer_VerwendetAusschliesslichGespeicherteSnapshots()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung.pdf", "%PDF-synthetisch"u8);
        var rechnung = TestData.GespeicherteRechnung();
        rechnung.Kunde = TestData.Kunde();
        rechnung.Kunde.Email = "aktuell@example.test";
        rechnung.EmpfaengerSnapshot!.Email = "snapshot@example.test";
        rechnung.Absender = TestData.FirmaProfil();
        rechnung.Absender.Name = "Aktueller Absender";
        rechnung.AbsenderSnapshot!.Name = "Historischer Absender";

        var entwurf = new EmailEntwurfComposer().Erstelle(rechnung, pdfPfad);

        Assert.Equal("snapshot@example.test", entwurf.Empfaenger);
        Assert.Equal("Rechnung 2026-001 – Historischer Absender", entwurf.Betreff);
        Assert.Contains("Rechnung 2026-001", entwurf.Nachricht);
        Assert.Contains("Historischer Absender", entwurf.Nachricht);
        Assert.DoesNotContain("aktuell@example.test", entwurf.Nachricht);
        Assert.DoesNotContain("Aktueller Absender", entwurf.Nachricht);
    }

    [Fact]
    public void EmailEntwurf_LehntCrLfUndFehlendenPdfAnhangAb()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung.pdf", "%PDF-synthetisch"u8);
        var leerePdf = verzeichnis.ErstelleDatei("leer.pdf", []);

        Assert.Throws<ArgumentException>(() => new EmailEntwurf(
            "kunde@example.test\r\nBlindkopie@example.test",
            "Rechnung",
            "Text",
            pdfPfad));
        Assert.Throws<ArgumentException>(() => new EmailEntwurf(
            "kunde@example.test",
            "Rechnung\nManipuliert",
            "Text",
            pdfPfad));
        Assert.Throws<ArgumentException>(() => new EmailEntwurf(
            "ungueltig",
            "Rechnung",
            "Text",
            pdfPfad));
        Assert.Throws<ArgumentException>(() => new EmailEntwurf(
            "kunde@example.test",
            "Rechnung",
            "Text",
            leerePdf));
        Assert.Throws<ArgumentException>(() => new EmailEntwurf(
            "kunde@example.test",
            "Rechnung",
            "Text",
            "rechnung.pdf"));
    }

    [Fact]
    public void EmailEntwurfComposer_LehntFehlendeGespeicherteDatenAb()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung.pdf", "%PDF-synthetisch"u8);
        var ohneEmpfaenger = TestData.GespeicherteRechnung();
        ohneEmpfaenger.EmpfaengerSnapshot = null;
        var ohneEmail = TestData.GespeicherteRechnung();
        ohneEmail.EmpfaengerSnapshot!.Email = string.Empty;
        var ohneAbsender = TestData.GespeicherteRechnung();
        ohneAbsender.AbsenderSnapshot = null;

        var composer = new EmailEntwurfComposer();

        Assert.Throws<ArgumentException>(() => composer.Erstelle(ohneEmpfaenger, pdfPfad));
        Assert.Throws<ArgumentException>(() => composer.Erstelle(ohneEmail, pdfPfad));
        Assert.Throws<ArgumentException>(() => composer.Erstelle(ohneAbsender, pdfPfad));
    }

    [Fact]
    public void MailtoUriBuilder_KodiertUtf8SonderzeichenUndCrLfOhneAnhangParameter()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung.pdf", "%PDF-synthetisch"u8);
        var entwurf = new EmailEntwurf(
            "kunde@example.test",
            "Änderung & Rückfrage? #100%",
            "Grüße & Rückfrage?\nZeile #2 mit 100%",
            pdfPfad);

        var uriText = MailtoUriBuilder.Erstelle(entwurf).OriginalString;

        Assert.StartsWith("mailto:", uriText, StringComparison.Ordinal);
        Assert.Contains("%C3%84", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%C3%BC", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%26", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%3F", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%23", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%25", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%0D%0A", uriText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attachment", uriText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmailStrategie_BeiClassicErfolgWirdKeinFallbackGestartet()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var entwurf = ErstelleEmailEntwurf(verzeichnis);
        var classic = new StubClassicOutlookOeffner
        {
            Ergebnis = EmailOeffnungsversuch.Geoeffnet
        };
        var mailto = new StubMailtoOeffner();
        var service = new WindowsEmailEntwurfService(classic, mailto);

        var ergebnis = service.Oeffne(entwurf);

        Assert.Equal(EmailEntwurfErgebnisStatus.MitAnhangGeoeffnet, ergebnis.Status);
        Assert.Equal(1, classic.Aufrufe);
        Assert.Equal(0, mailto.Aufrufe);
    }

    [Fact]
    public void EmailStrategie_BeiNichtVerfuegbaremClassicOutlookWirdMailtoVerwendet()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var entwurf = ErstelleEmailEntwurf(verzeichnis);
        var classic = new StubClassicOutlookOeffner
        {
            Ergebnis = EmailOeffnungsversuch.NichtVerfuegbar
        };
        var mailto = new StubMailtoOeffner();
        var service = new WindowsEmailEntwurfService(classic, mailto);

        var ergebnis = service.Oeffne(entwurf);

        Assert.Equal(EmailEntwurfErgebnisStatus.OhneAnhangAngefordert, ergebnis.Status);
        Assert.Equal(1, classic.Aufrufe);
        Assert.Equal(1, mailto.Aufrufe);
        Assert.NotNull(mailto.LetzteUri);
        Assert.DoesNotContain(
            "attachment",
            mailto.LetzteUri.OriginalString,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmailStrategie_BeiClassicFehlerWirdEbenfallsMailtoVerwendet()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var entwurf = ErstelleEmailEntwurf(verzeichnis);
        var classic = new StubClassicOutlookOeffner
        {
            Ergebnis = EmailOeffnungsversuch.Fehlgeschlagen
        };
        var mailto = new StubMailtoOeffner();

        var ergebnis = new WindowsEmailEntwurfService(classic, mailto).Oeffne(entwurf);

        Assert.Equal(EmailEntwurfErgebnisStatus.OhneAnhangAngefordert, ergebnis.Status);
        Assert.Equal(1, mailto.Aufrufe);
    }

    [Fact]
    public void WindowsMailtoEntwurfOeffner_VerwendetShellExecuteMitGefaktemProzessstart()
    {
        var prozessStarter = new StubProzessStarter();
        var oeffner = new WindowsMailtoEntwurfOeffner(prozessStarter);
        var uri = new Uri("mailto:kunde@example.test?subject=Test&body=Text", UriKind.Absolute);

        var ergebnis = oeffner.Oeffne(uri);

        Assert.Equal(EmailOeffnungsversuch.Geoeffnet, ergebnis);
        var startInfo = Assert.Single(prozessStarter.Aufrufe);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal(uri.OriginalString, startInfo.FileName);
        Assert.Empty(startInfo.ArgumentList);
    }

    [Fact]
    public void WindowsDateiOeffner_VerwendetSichereArgumentlisteFuerExplorer()
    {
        using var verzeichnis = new TempTestVerzeichnis();
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung & test.pdf", "%PDF-synthetisch"u8);
        var prozessStarter = new StubProzessStarter();
        var oeffner = new WindowsDateiOeffner(prozessStarter);

        oeffner.Oeffne(pdfPfad);
        oeffner.ImExplorerAnzeigen(pdfPfad);

        Assert.Collection(
            prozessStarter.Aufrufe,
            viewer =>
            {
                Assert.True(viewer.UseShellExecute);
                Assert.Equal(pdfPfad, viewer.FileName);
                Assert.Empty(viewer.ArgumentList);
            },
            explorer =>
            {
                Assert.True(explorer.UseShellExecute);
                Assert.Equal("explorer.exe", explorer.FileName);
                Assert.Equal(["/select,", pdfPfad], explorer.ArgumentList);
            });
    }

    private static EmailEntwurf ErstelleEmailEntwurf(TempTestVerzeichnis verzeichnis)
    {
        var pdfPfad = verzeichnis.ErstelleDatei("rechnung.pdf", "%PDF-synthetisch"u8);
        return new EmailEntwurf(
            "kunde@example.test",
            "Rechnung 2026-001",
            "Guten Tag,\r\n\r\nanbei die Rechnung.",
            pdfPfad);
    }
}
