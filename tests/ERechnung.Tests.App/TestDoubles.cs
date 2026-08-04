using System.Diagnostics;
using System.IO;
using ERechnung.App.Services;
using ERechnung.App.ViewModels;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.App;

internal sealed class StubRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items;

    public StubRepository(IEnumerable<T>? items = null)
    {
        _items = items?.ToList() ?? [];
    }

    public Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

    public Task<List<T>> GetAllAsync() => Task.FromResult(_items.ToList());

    public Task<T> CreateAsync(T entity)
    {
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(T entity) => Task.CompletedTask;

    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class StubRechnungRepository : IRechnungRepository
{
    private readonly List<Rechnung> _rechnungen;
    private int _naechsteId = 100;

    public StubRechnungRepository(IEnumerable<Rechnung>? rechnungen = null)
    {
        _rechnungen = rechnungen?.ToList() ?? [];
    }

    public string? LetzterStatusFilter { get; private set; }
    public int CreateAufrufe { get; private set; }
    public int UpdateAufrufe { get; private set; }
    public int SetPdfAufrufe { get; private set; }

    public Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null)
    {
        LetzterStatusFilter = status;
        IReadOnlyList<RechnungsUebersicht> result = _rechnungen
            .Where(rechnung => status is null || rechnung.Status == status)
            .Select(rechnung => new RechnungsUebersicht
            {
                Id = rechnung.Id!.Value,
                Nummer = rechnung.Nummer,
                Rechnungsdatum = rechnung.Rechnungsdatum,
                KundeName = rechnung.EmpfaengerSnapshot?.Name
                    ?? rechnung.Kunde?.Firmenname
                    ?? string.Empty,
                GesamtbetragBrutto = rechnung.GesamtbetragBrutto,
                Status = rechnung.Status,
                GeaendertAm = rechnung.GeaendertAm,
                PdfRelativerPfad = rechnung.PdfVerknuepfung?.RelativerPfad,
                PdfErstelltAm = rechnung.PdfVerknuepfung?.ErstelltAm,
                PdfRechnungsstandAm = rechnung.PdfVerknuepfung?.RechnungsstandAm
            })
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Rechnung?> GetByIdAsync(int id) =>
        Task.FromResult(_rechnungen.FirstOrDefault(rechnung => rechnung.Id == id));

    public Task<Rechnung> CreateAsync(Rechnung rechnung)
    {
        CreateAufrufe++;
        rechnung.Id = _naechsteId++;
        rechnung.Nummer = $"{rechnung.Rechnungsdatum:yyyy}-999";
        _rechnungen.Add(rechnung);
        return Task.FromResult(rechnung);
    }

    public Task UpdateAsync(Rechnung rechnung)
    {
        UpdateAufrufe++;
        var index = _rechnungen.FindIndex(item => item.Id == rechnung.Id);
        if (index >= 0)
        {
            _rechnungen[index] = rechnung;
        }

        return Task.CompletedTask;
    }

    public Task SetPdfVerknuepfungAsync(
        int id,
        RechnungsPdfVerknuepfung verknuepfung,
        DateTime erwartetGeaendertAm,
        RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
    {
        SetPdfAufrufe++;
        var rechnung = _rechnungen.FirstOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("Die Rechnung wurde nicht gefunden.");
        if (rechnung.GeaendertAm != erwartetGeaendertAm
            || !PdfVerknuepfungenSindGleich(
                rechnung.PdfVerknuepfung,
                erwartetePdfVerknuepfung))
        {
            throw new InvalidOperationException("Der Rechnungsstand wurde zwischenzeitlich geändert.");
        }

        rechnung.PdfVerknuepfung = verknuepfung;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        _rechnungen.RemoveAll(rechnung => rechnung.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteIfUnchangedAsync(
        int id,
        DateTime erwartetGeaendertAm,
        RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
    {
        var rechnung = _rechnungen.FirstOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("Die Rechnung wurde nicht gefunden.");
        if (rechnung.GeaendertAm != erwartetGeaendertAm
            || !PdfVerknuepfungenSindGleich(
                rechnung.PdfVerknuepfung,
                erwartetePdfVerknuepfung))
        {
            throw new InvalidOperationException("Der Rechnungsstand wurde zwischenzeitlich geändert.");
        }

        _rechnungen.Remove(rechnung);
        return Task.CompletedTask;
    }

    private static bool PdfVerknuepfungenSindGleich(
        RechnungsPdfVerknuepfung? links,
        RechnungsPdfVerknuepfung? rechts)
    {
        return ReferenceEquals(links, rechts)
               || links is not null
               && rechts is not null
               && links.RelativerPfad == rechts.RelativerPfad
               && links.ErstelltAm == rechts.ErstelltAm
               && links.RechnungsstandAm == rechts.RechnungsstandAm;
    }
}

internal sealed class StubDialogService : IUserDialogService
{
    public bool Bestaetigen { get; set; } = true;
    public List<(string Message, string Title)> Bestaetigungen { get; } = [];
    public List<(string Message, string Title)> Informationen { get; } = [];
    public List<(string Message, string Title)> Fehler { get; } = [];

    public bool Confirm(string message, string title)
    {
        Bestaetigungen.Add((message, title));
        return Bestaetigen;
    }

    public void ShowInfo(string message, string title) => Informationen.Add((message, title));

    public void ShowError(string message, string title) => Fehler.Add((message, title));
}

internal sealed class TempTestVerzeichnis : IDisposable
{
    public TempTestVerzeichnis()
    {
        Pfad = Path.Combine(
            Path.GetTempPath(),
            "ERechnung-Tests-App",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Pfad);
    }

    public string Pfad { get; }

    public string ErstelleDatei(string dateiname, ReadOnlySpan<byte> inhalt)
    {
        var dateiPfad = Path.Combine(Pfad, dateiname);
        File.WriteAllBytes(dateiPfad, inhalt.ToArray());
        return dateiPfad;
    }

    public void Dispose()
    {
        if (Directory.Exists(Pfad))
        {
            Directory.Delete(Pfad, recursive: true);
        }
    }
}

internal sealed class StubPdfGenerator : IRechnungsPdfGenerator
{
    public int Aufrufe { get; private set; }

    public byte[] Erzeuge(Rechnung rechnung, DateTimeOffset erzeugtAm)
    {
        Aufrufe++;
        return "%PDF-synthetisch"u8.ToArray();
    }
}

internal sealed class StubPdfAblage : IRechnungsPdfAblage, IDisposable
{
    private readonly string _basisPfad = Path.Combine(
        Path.GetTempPath(),
        "ERechnung-Tests-App",
        Guid.NewGuid().ToString("N"));

    public int SpeicherAufrufe { get; private set; }

    public async Task<string> SpeichereAsync(
        Rechnung rechnung,
        ReadOnlyMemory<byte> pdfInhalt,
        CancellationToken cancellationToken)
    {
        SpeicherAufrufe++;
        var relativerPfad = $"{rechnung.Rechnungsdatum:yyyy}/rechnung-{rechnung.Id}.pdf";
        var vollstaendigerPfad = LoeseVollstaendigenPfadAuf(relativerPfad);
        Directory.CreateDirectory(Path.GetDirectoryName(vollstaendigerPfad)!);
        await File.WriteAllBytesAsync(vollstaendigerPfad, pdfInhalt.ToArray(), cancellationToken);
        return relativerPfad;
    }

    public bool Existiert(string relativerPfad) => File.Exists(LoeseVollstaendigenPfadAuf(relativerPfad));

    public string LoeseVollstaendigenPfadAuf(string relativerPfad)
    {
        if (string.IsNullOrWhiteSpace(relativerPfad) || Path.IsPathFullyQualified(relativerPfad))
        {
            throw new ArgumentException("Der Pfad muss relativ sein.", nameof(relativerPfad));
        }

        var vollstaendigerPfad = Path.GetFullPath(
            Path.Combine(_basisPfad, relativerPfad.Replace('/', Path.DirectorySeparatorChar)));
        var basisPrefix = Path.GetFullPath(_basisPfad) + Path.DirectorySeparatorChar;
        if (!vollstaendigerPfad.StartsWith(basisPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Der Pfad verlässt die Testablage.", nameof(relativerPfad));
        }

        return vollstaendigerPfad;
    }

    public Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(LoeseVollstaendigenPfadAuf(relativerPfad));
        return Task.CompletedTask;
    }

    public void FuegePdfHinzu(string relativerPfad)
    {
        var vollstaendigerPfad = LoeseVollstaendigenPfadAuf(relativerPfad);
        Directory.CreateDirectory(Path.GetDirectoryName(vollstaendigerPfad)!);
        File.WriteAllBytes(vollstaendigerPfad, "%PDF-synthetisch"u8.ToArray());
    }

    public void Dispose()
    {
        if (Directory.Exists(_basisPfad))
        {
            Directory.Delete(_basisPfad, recursive: true);
        }
    }
}

internal sealed class StubDateiOeffner : IDateiOeffner
{
    public List<string> GeoeffneteDateien { get; } = [];
    public List<string> ImExplorerAngezeigteDateien { get; } = [];

    public void Oeffne(string dateiPfad) => GeoeffneteDateien.Add(dateiPfad);

    public void ImExplorerAnzeigen(string dateiPfad) => ImExplorerAngezeigteDateien.Add(dateiPfad);
}

internal sealed class StubEmailEntwurfService : IEmailEntwurfService
{
    public EmailEntwurfErgebnis Ergebnis { get; set; } = new(
        EmailEntwurfErgebnisStatus.MitAnhangGeoeffnet);
    public List<EmailEntwurf> Entwuerfe { get; } = [];

    public EmailEntwurfErgebnis Oeffne(EmailEntwurf entwurf)
    {
        Entwuerfe.Add(entwurf);
        return Ergebnis;
    }
}

internal sealed class StubClassicOutlookOeffner : IClassicOutlookEntwurfOeffner
{
    public EmailOeffnungsversuch Ergebnis { get; set; } = EmailOeffnungsversuch.NichtVerfuegbar;
    public int Aufrufe { get; private set; }

    public EmailOeffnungsversuch Oeffne(EmailEntwurf entwurf)
    {
        Aufrufe++;
        return Ergebnis;
    }
}

internal sealed class StubMailtoOeffner : IMailtoEntwurfOeffner
{
    public EmailOeffnungsversuch Ergebnis { get; set; } = EmailOeffnungsversuch.Geoeffnet;
    public int Aufrufe { get; private set; }
    public Uri? LetzteUri { get; private set; }

    public EmailOeffnungsversuch Oeffne(Uri mailtoUri)
    {
        Aufrufe++;
        LetzteUri = mailtoUri;
        return Ergebnis;
    }
}

internal sealed class StubProzessStarter : IProzessStarter
{
    public List<ProcessStartInfo> Aufrufe { get; } = [];

    public void Starte(ProcessStartInfo startInfo) => Aufrufe.Add(startInfo);
}

internal static class TestViewModelFactory
{
    public static RechnungsUebersichtViewModel ErstelleUebersicht(
        StubRechnungRepository repository,
        StubDialogService? dialogService = null,
        IRechnungsPdfAblage? pdfAblage = null,
        StubPdfGenerator? pdfGenerator = null,
        StubDateiOeffner? dateiOeffner = null,
        StubEmailEntwurfService? emailService = null,
        ERechnung.Core.Services.IUblGenerator? ublGenerator = null)
    {
        dialogService ??= new StubDialogService();
        pdfAblage ??= new StubPdfAblage();
        pdfGenerator ??= new StubPdfGenerator();
        dateiOeffner ??= new StubDateiOeffner();
        emailService ??= new StubEmailEntwurfService();
        ublGenerator ??= new ERechnung.XML.Generators.UblGenerator();
        return new RechnungsUebersichtViewModel(
            new RechnungService(repository, pdfAblage: pdfAblage),
            new RechnungsPdfService(repository, pdfGenerator, pdfAblage),
            pdfAblage,
            dateiOeffner,
            new EmailEntwurfComposer(),
            emailService,
            ublGenerator,
            dialogService);
    }
}

internal static class TestData
{
    public static Kunde Kunde(int id = 1) => new()
    {
        Id = id,
        Firmenname = "Musterkunde GmbH",
        Ansprechpartner = "Erika Beispiel",
        Strasse = "Kundenweg 1",
        PLZ = "10115",
        Ort = "Berlin",
        Land = "DE",
        Email = "kunde@example.test"
    };

    public static FirmaProfil FirmaProfil(int id = 2) => new()
    {
        Id = id,
        Name = "Rechnungssteller GmbH",
        Ansprechpartner = "Max Beispiel",
        Strasse = "Firmenweg 2",
        PLZ = "20095",
        Ort = "Hamburg",
        Land = "DE",
        Email = "firma@example.test",
        IBAN = "DE02120300000000202051",
        BIC = "BYLADEM1001"
    };

    public static Rechnung GespeicherteRechnung(
        int id = 10,
        string status = RechnungsStatus.Erstellt)
    {
        var kunde = Kunde();
        var profil = FirmaProfil();
        return new Rechnung
        {
            Id = id,
            Nummer = "2026-001",
            Titel = "Testrechnung",
            Rechnungsdatum = new DateTime(2026, 8, 1),
            Leistungsdatum = new DateTime(2026, 7, 31),
            Faeligkeitsdatum = new DateTime(2026, 8, 15),
            KundeId = kunde.Id,
            FirmaProfilId = profil.Id,
            Status = status,
            Waehrung = "EUR",
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = kunde.Id!.Value,
                Name = kunde.Firmenname,
                Land = kunde.Land,
                Email = kunde.Email
            },
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = profil.Id!.Value,
                Name = profil.Name,
                Land = profil.Land,
                Email = profil.Email
            },
            Positionen =
            [
                new RechnungsPosition
                {
                    Beschreibung = "Beratung",
                    Menge = 1m,
                    Einheit = "ST",
                    EinzelpreisNetto = 100m,
                    Steuersatz = 19m
                }
            ]
        };
    }
}
