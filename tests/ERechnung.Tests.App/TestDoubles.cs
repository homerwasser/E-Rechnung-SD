using ERechnung.App.Services;
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
                Status = rechnung.Status
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

    public Task DeleteAsync(int id)
    {
        _rechnungen.RemoveAll(rechnung => rechnung.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class StubDialogService : IUserDialogService
{
    public bool Bestaetigen { get; set; } = true;
    public List<(string Message, string Title)> Bestaetigungen { get; } = [];
    public List<(string Message, string Title)> Fehler { get; } = [];

    public bool Confirm(string message, string title)
    {
        Bestaetigungen.Add((message, title));
        return Bestaetigen;
    }

    public void ShowError(string message, string title) => Fehler.Add((message, title));
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
            Faeligkeitsdatum = new DateTime(2026, 8, 15),
            KundeId = kunde.Id,
            FirmaProfilId = profil.Id,
            Status = status,
            Waehrung = "EUR",
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = kunde.Id!.Value,
                Name = kunde.Firmenname,
                Land = kunde.Land
            },
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = profil.Id!.Value,
                Name = profil.Name,
                Land = profil.Land
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
