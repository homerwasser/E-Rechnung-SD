using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ERechnung.Core.Models;
using ERechnung.PDF;
using ERechnung.PDF.Generators;
using QuestPDF.Infrastructure;

namespace ERechnung.Tests.Unit;

public sealed class QuestPdfRechnungsPdfGeneratorTests
{
    private static readonly DateTimeOffset ErzeugtAm = new(
        2026,
        8,
        3,
        10,
        15,
        30,
        TimeSpan.Zero);

    [Fact]
    public void KonfiguriereCommunity_MehrfachAufgerufen_BleibtCommunityLizenz()
    {
        QuestPdfLizenz.KonfiguriereCommunity();
        QuestPdfLizenz.KonfiguriereCommunity();

        Assert.Equal(LicenseType.Community, QuestPDF.Settings.License);
    }

    [Fact]
    public void Erzeuge_OhneLogo_LiefertGueltigePdfSignaturUndSinnvolleGroesse()
    {
        var rechnung = ErstelleGueltigeRechnung();

        var pdf = new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm);

        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.True(pdf.Length > 5_000, $"Die PDF ist mit {pdf.Length} Bytes unerwartet klein.");
    }

    [Fact]
    public void Erzeuge_MitProgrammgeneriertemMiniPng_LiefertPdf()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.AbsenderSnapshot!.LogoMedientyp = "image/png";
        rechnung.AbsenderSnapshot.LogoInhalt = ErstelleMiniPng(31, 78, 120, 255);

        var pdf = new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm);

        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.True(pdf.Length > 5_000);
    }

    [Fact]
    public void Erzeuge_MitUngueltigenLogoBytes_LaesstLogoWegUndLiefertPdf()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.AbsenderSnapshot!.LogoMedientyp = "image/png";
        rechnung.AbsenderSnapshot.LogoInhalt = [0x89, 0x50, 0x4E, 0x47, 0x00, 0x01];

        var pdf = new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm);

        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.True(pdf.Length > 5_000);
    }

    [Fact]
    public void Erzeuge_MitNichtUnterstuetztemLogoMedientyp_LaesstLogoWeg()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.AbsenderSnapshot!.LogoMedientyp = "image/gif";
        rechnung.AbsenderSnapshot.LogoInhalt = [1, 2, 3, 4];

        var pdf = new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm);

        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
    }

    [Fact]
    public void Layoutdaten_VerwendenSnapshotsDatumUndGespeichertenWaehrungscode()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.Waehrung = "CHF";
        rechnung.Leistungsdatum = new DateTime(2026, 7, 31);
        rechnung.Rechnungsdatum = new DateTime(2026, 8, 3);
        rechnung.Absender = new FirmaProfil
        {
            Name = "Aktueller, nicht zu verwendender Absender",
            LogoPfad = @"C:\private\aktuelles-logo.png"
        };
        rechnung.Kunde = new Kunde
        {
            Firmenname = "Aktueller, nicht zu verwendender Empfänger",
            Strasse = "Andere Straße 99"
        };

        var daten = RechnungsPdfLayoutDaten.Erstelle(rechnung, ErzeugtAm);

        Assert.Equal("Snapshot Absender GmbH", daten.Absender.Name);
        Assert.Equal("Snapshot Empfänger AG", daten.Empfaenger.Name);
        Assert.Equal("Snapshotstraße 1", daten.Empfaenger.Strasse);
        Assert.Equal("31.07.2026", daten.FormatiereDatum(daten.Leistungsdatum!.Value));
        Assert.Equal("03.08.2026", daten.FormatiereDatum(daten.Rechnungsdatum));
        Assert.Equal("CHF", daten.Waehrung);
        Assert.Equal("1.234,50 CHF", daten.FormatiereGeldbetrag(1_234.5m));
    }

    [Fact]
    public void Layoutdaten_BildenGetrennteSteuergruppenFuerNullSiebenUndNeunzehnProzent()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.Positionen =
        [
            ErstellePosition("Steuerfreie Leistung", 1m, 10m, 0m),
            ErstellePosition("Ermäßigte Leistung", 2m, 10m, 7m),
            ErstellePosition("Reguläre Leistung", 1m, 100m, 19m)
        ];

        var daten = RechnungsPdfLayoutDaten.Erstelle(rechnung, ErzeugtAm);

        Assert.Equal(
            [
                new RechnungsPdfSteuergruppenDaten(0m, 10m, 0m),
                new RechnungsPdfSteuergruppenDaten(7m, 20m, 1.40m),
                new RechnungsPdfSteuergruppenDaten(19m, 100m, 19m)
            ],
            daten.Steuergruppen);
        Assert.Equal(130m, daten.Nettosumme);
        Assert.Equal(150.40m, daten.Bruttosumme);
    }

    [Fact]
    public void Dokumentkonfiguration_IstPdfA3bKomprimiertUndLinksNachRechts()
    {
        var einstellungen = QuestPdfRechnungsPdfGenerator.ErstelleDokumenteinstellungen();

        Assert.Equal(PDFA_Conformance.PDFA_3B, einstellungen.PDFA_Conformance);
        Assert.True(einstellungen.CompressDocument);
        Assert.Equal(ImageCompressionQuality.High, einstellungen.ImageCompressionQuality);
        Assert.Equal(144, einstellungen.ImageRasterDpi);
        Assert.Equal(ContentDirection.LeftToRight, einstellungen.ContentDirection);
    }

    [Fact]
    public void Metadaten_EnthaltenNurUnkritischeRechnungsdaten()
    {
        var rechnung = ErstelleGueltigeRechnung();
        rechnung.AbsenderSnapshot!.LogoPfad = @"C:\private\snapshot-logo.png";
        rechnung.EmpfaengerSnapshot!.Email = "vertraulich@example.invalid";
        var daten = RechnungsPdfLayoutDaten.Erstelle(rechnung, ErzeugtAm);

        var metadaten = QuestPdfRechnungsPdfGenerator.ErstelleMetadaten(daten);
        var metadatenText = string.Join(
            "|",
            new[]
            {
                metadaten.Title,
                metadaten.Author,
                metadaten.Subject,
                metadaten.Keywords,
                metadaten.Creator,
                metadaten.Producer,
                metadaten.Language
            }.Where(wert => !string.IsNullOrEmpty(wert)));

        Assert.Equal("Rechnung RE-2026-0042", metadaten.Title);
        Assert.Equal("Snapshot Absender GmbH", metadaten.Author);
        Assert.Equal("de-DE", metadaten.Language);
        Assert.Equal(ErzeugtAm, metadaten.CreationDate);
        Assert.DoesNotContain("vertraulich@example.invalid", metadatenText, StringComparison.Ordinal);
        Assert.DoesNotContain("Snapshotstraße 1", metadatenText, StringComparison.Ordinal);
        Assert.DoesNotContain("snapshot-logo.png", metadatenText, StringComparison.Ordinal);
    }

    [Fact]
    public void Erzeuge_MitMehrAlsHundertPositionenUndLangenTexten_LiefertMehrseitigePdf()
    {
        var rechnung = ErstelleGueltigeRechnung();
        var langeBeschreibung = string.Join(
            " ",
            Enumerable.Repeat("Ausführliche synthetische Leistungsbeschreibung", 80));
        rechnung.Bemerkung = string.Join(
            " ",
            Enumerable.Repeat("Lange synthetische Bemerkung mit Zahlungs- und Leistungsdetails.", 100));
        rechnung.Positionen = Enumerable
            .Range(1, 120)
            .Select(index => ErstellePosition(
                index == 1
                    ? langeBeschreibung
                    : $"Synthetische Position {index}: " + string.Join(
                        " ",
                        Enumerable.Repeat("ausführlicher Inhalt", 8)),
                1m + index / 10m,
                10m + index,
                index % 3 switch
                {
                    0 => 0m,
                    1 => 7m,
                    _ => 19m
                }))
            .ToList();

        var pdf = new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm);

        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.True(pdf.Length > 20_000, $"Die mehrseitige PDF ist mit {pdf.Length} Bytes unerwartet klein.");
    }

    [Theory]
    [InlineData("id", "Rechnungs-ID")]
    [InlineData("nummer", "Rechnungsnummer")]
    [InlineData("absender", "Absender-Snapshot")]
    [InlineData("empfaenger", "Empfänger-Snapshot")]
    [InlineData("positionen", "echte Rechnungsposition")]
    [InlineData("platzhalter-position", "echte Rechnungsposition")]
    public void Erzeuge_MitUnvollstaendigerGespeicherterRechnung_LehntVerstaendlichAb(
        string fehlerfall,
        string erwarteterMeldungsteil)
    {
        var rechnung = ErstelleGueltigeRechnung();
        switch (fehlerfall)
        {
            case "id":
                rechnung.Id = null;
                break;
            case "nummer":
                rechnung.Nummer = " ";
                break;
            case "absender":
                rechnung.AbsenderSnapshot = null;
                break;
            case "empfaenger":
                rechnung.EmpfaengerSnapshot = null;
                break;
            case "positionen":
                rechnung.Positionen = [];
                break;
            case "platzhalter-position":
                rechnung.Positionen = [new RechnungsPosition { Beschreibung = " ", Menge = 0m }];
                break;
        }

        var fehler = Assert.Throws<InvalidOperationException>(
            () => new QuestPdfRechnungsPdfGenerator().Erzeuge(rechnung, ErzeugtAm));

        Assert.Contains(erwarteterMeldungsteil, fehler.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Rechnung ErstelleGueltigeRechnung()
    {
        return new Rechnung
        {
            Id = 42,
            Nummer = "RE-2026-0042",
            Titel = "Rechnung für Veranstaltungsleistungen",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            Leistungsdatum = new DateTime(2026, 8, 1),
            Faeligkeitsdatum = new DateTime(2026, 8, 17),
            Waehrung = "EUR",
            Status = "Interner Status darf nicht ausgegeben werden",
            Bemerkung = "Vielen Dank für Ihren Auftrag.",
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = 21,
                Name = "Snapshot Absender GmbH",
                Ansprechpartner = "Erika Muster",
                Strasse = "Absenderweg 7",
                PLZ = "10115",
                Ort = "Berlin",
                Land = "DE",
                Email = "rechnung@example.invalid",
                Telefon = "+49 30 123456",
                UstIdNr = "DE123456789",
                IBAN = "DE02120300000000202051",
                BIC = "BYLADEM1001",
                LogoPfad = @"C:\private\darf-nicht-verwendet-werden.png"
            },
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = 11,
                Name = "Snapshot Empfänger AG",
                Ansprechpartner = "Max Mustermann",
                Strasse = "Snapshotstraße 1",
                PLZ = "20095",
                Ort = "Hamburg",
                Land = "DE",
                Email = "empfang@example.invalid",
                UstIdNr = "DE987654321"
            },
            Positionen =
            [
                ErstellePosition("Synthetische Beratungsleistung", 2.5m, 100m, 19m)
            ]
        };
    }

    private static RechnungsPosition ErstellePosition(
        string beschreibung,
        decimal menge,
        decimal einzelpreisNetto,
        decimal steuersatz)
    {
        return new RechnungsPosition
        {
            Beschreibung = beschreibung,
            Menge = menge,
            Einheit = "Std.",
            EinzelpreisNetto = einzelpreisNetto,
            Steuersatz = steuersatz
        };
    }

    private static byte[] ErstelleMiniPng(byte rot, byte gruen, byte blau, byte alpha)
    {
        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);

        var kopf = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(kopf.AsSpan(0, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(kopf.AsSpan(4, 4), 1);
        kopf[8] = 8;
        kopf[9] = 6;
        SchreibePngBlock(png, "IHDR", kopf);

        using var komprimierteBilddaten = new MemoryStream();
        using (var zlib = new ZLibStream(
                   komprimierteBilddaten,
                   CompressionLevel.SmallestSize,
                   leaveOpen: true))
        {
            zlib.Write([0, rot, gruen, blau, alpha]);
        }

        SchreibePngBlock(png, "IDAT", komprimierteBilddaten.ToArray());
        SchreibePngBlock(png, "IEND", []);
        return png.ToArray();
    }

    private static void SchreibePngBlock(Stream ziel, string typ, ReadOnlySpan<byte> daten)
    {
        Span<byte> laenge = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(laenge, (uint)daten.Length);
        ziel.Write(laenge);

        var typBytes = Encoding.ASCII.GetBytes(typ);
        ziel.Write(typBytes);
        ziel.Write(daten);

        var pruefdaten = new byte[typBytes.Length + daten.Length];
        typBytes.CopyTo(pruefdaten, 0);
        daten.CopyTo(pruefdaten.AsSpan(typBytes.Length));

        Span<byte> pruefsumme = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(pruefsumme, BerechneCrc32(pruefdaten));
        ziel.Write(pruefsumme);
    }

    private static uint BerechneCrc32(ReadOnlySpan<byte> daten)
    {
        var crc = uint.MaxValue;
        foreach (var wert in daten)
        {
            crc ^= wert;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xEDB88320u
                    : crc >> 1;
            }
        }

        return ~crc;
    }
}
