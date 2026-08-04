using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class RechnungServiceTests
{
    [Fact]
    public async Task SpeichernAsync_ForCreate_CalculatesAndCreatesSnapshots()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: null, nummer: string.Empty);
        rechnung.GesamtbetragNetto = 999m;
        rechnung.UmsatzsteuerBetrag = 999m;
        rechnung.GesamtbetragBrutto = 999m;
        rechnung.ErstelltAm = default;
        rechnung.GeaendertAm = default;
        var vorbefuellterEmpfaenger = new RechnungsEmpfaengerSnapshot
        {
            QuellId = rechnung.KundeId!.Value,
            Name = "Vorbefüllter Empfänger"
        };
        var vorbefuellterAbsender = new RechnungsAbsenderSnapshot
        {
            QuellId = rechnung.FirmaProfilId!.Value,
            Name = "Vorbefüllter Absender"
        };
        rechnung.EmpfaengerSnapshot = vorbefuellterEmpfaenger;
        rechnung.AbsenderSnapshot = vorbefuellterAbsender;

        var result = await service.SpeichernAsync(rechnung);

        Assert.Same(rechnung, result);
        Assert.Equal(1, repository.CreateCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
        Assert.Equal("RE-2026-0001", result.Nummer);
        Assert.Equal(200m, result.GesamtbetragNetto);
        Assert.Equal(38m, result.UmsatzsteuerBetrag);
        Assert.Equal(238m, result.GesamtbetragBrutto);
        Assert.Equal(19m, result.GesamtsteuerRate);
        Assert.NotEqual(default, result.ErstelltAm);
        Assert.Equal(result.ErstelltAm, result.GeaendertAm);

        var empfaenger = Assert.IsType<RechnungsEmpfaengerSnapshot>(result.EmpfaengerSnapshot);
        Assert.NotSame(vorbefuellterEmpfaenger, empfaenger);
        Assert.Equal(11, empfaenger.QuellId);
        Assert.Equal("Synthetischer Kunde GmbH", empfaenger.Name);
        Assert.Equal("DE123456789", empfaenger.UstIdNr);

        var absender = Assert.IsType<RechnungsAbsenderSnapshot>(result.AbsenderSnapshot);
        Assert.NotSame(vorbefuellterAbsender, absender);
        Assert.Equal(21, absender.QuellId);
        Assert.Equal("Synthetischer Absender GmbH", absender.Name);
        Assert.Equal("DE12500105170648489890", absender.IBAN);
        Assert.Equal("+49 30 123456", absender.Telefon);
    }

    [Fact]
    public async Task SpeichernAsync_ForCreate_WithMismatchedSourceIds_ReturnsAllErrors()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: null, nummer: string.Empty);
        rechnung.Kunde!.Id = 12;
        rechnung.Absender!.Id = 22;

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains(
            "Die ID der Kundenstammdaten stimmt nicht mit der ausgewählten Kunden-ID überein.",
            exception.Errors);
        Assert.Contains(
            "Die ID der Firmenprofil-Stammdaten stimmt nicht mit der ausgewählten Firmenprofil-ID überein.",
            exception.Errors);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task SpeichernAsync_ForUpdate_RequiresInvoiceNumber()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: string.Empty);

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Contains("Die Rechnungsnummer ist erforderlich.", exception.Errors);
        Assert.Contains("Die Rechnungsnummer ist erforderlich.", exception.Message);
        Assert.Equal(0, repository.CreateCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task SpeichernAsync_ForUpdate_DelegatesToRepository()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");

        var result = await service.SpeichernAsync(rechnung);

        Assert.Same(rechnung, result);
        Assert.Equal(0, repository.CreateCallCount);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Same(rechnung, repository.UpdatedInvoice);
    }

    [Fact]
    public async Task SpeichernAsync_ForUpdate_PreservesConcurrencyTimestampUntilRepositoryCall()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        var concurrencyTimestamp = new DateTime(2026, 7, 31, 12, 34, 56, DateTimeKind.Utc);
        rechnung.GeaendertAm = concurrencyTimestamp;

        await service.SpeichernAsync(rechnung);

        Assert.Equal(concurrencyTimestamp, repository.UpdatedGeaendertAm);
    }

    [Fact]
    public async Task SpeichernAsync_WithUnchangedSources_PreservesExistingSnapshots()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        var empfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = rechnung.KundeId!.Value,
            Name = "Historischer Kundenname"
        };
        var absenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = rechnung.FirmaProfilId!.Value,
            Name = "Historischer Absendername"
        };
        rechnung.EmpfaengerSnapshot = empfaengerSnapshot;
        rechnung.AbsenderSnapshot = absenderSnapshot;
        rechnung.Kunde!.Firmenname = "Geänderter Kundenname";
        rechnung.Absender!.Name = "Geänderter Absendername";

        await service.SpeichernAsync(rechnung);

        Assert.Same(empfaengerSnapshot, rechnung.EmpfaengerSnapshot);
        Assert.Equal("Historischer Kundenname", rechnung.EmpfaengerSnapshot.Name);
        Assert.Same(absenderSnapshot, rechnung.AbsenderSnapshot);
        Assert.Equal("Historischer Absendername", rechnung.AbsenderSnapshot.Name);
    }

    [Fact]
    public async Task SpeichernAsync_ForUpdate_WithMismatchedSourceIds_ReturnsAllErrors()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = rechnung.KundeId!.Value,
            Name = "Historischer Kundenname"
        };
        rechnung.AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = rechnung.FirmaProfilId!.Value,
            Name = "Historischer Absendername"
        };
        rechnung.Kunde!.Id = 12;
        rechnung.Absender!.Id = 22;

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains(
            "Die ID der Kundenstammdaten stimmt nicht mit der ausgewählten Kunden-ID überein.",
            exception.Errors);
        Assert.Contains(
            "Die ID der Firmenprofil-Stammdaten stimmt nicht mit der ausgewählten Firmenprofil-ID überein.",
            exception.Errors);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task SpeichernAsync_WithChangedSources_ReplacesSnapshots()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = 10,
            Name = "Alter Kunde"
        };
        rechnung.AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = 20,
            Name = "Alter Absender"
        };

        await service.SpeichernAsync(rechnung);

        Assert.Equal(11, rechnung.EmpfaengerSnapshot!.QuellId);
        Assert.Equal("Synthetischer Kunde GmbH", rechnung.EmpfaengerSnapshot.Name);
        Assert.Equal(21, rechnung.AbsenderSnapshot!.QuellId);
        Assert.Equal("Synthetischer Absender GmbH", rechnung.AbsenderSnapshot.Name);
    }

    [Fact]
    public async Task SpeichernAsync_ForUpdate_WithChangedSourceIds_RequiresSources()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = 10,
            Name = "Alter Kunde"
        };
        rechnung.AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = 20,
            Name = "Alter Absender"
        };
        rechnung.Kunde = null;
        rechnung.Absender = null;

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains(
            "Die Stammdaten des ausgewählten Kunden fehlen für den Rechnungsempfänger-Snapshot.",
            exception.Errors);
        Assert.Contains(
            "Die Stammdaten des ausgewählten Firmenprofils fehlen für den Rechnungsabsender-Snapshot.",
            exception.Errors);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task SpeichernAsync_ForCreate_WithPrefilledSnapshotsButWithoutSources_ReturnsAllErrors()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = CreateValidInvoice(id: null, nummer: string.Empty);
        rechnung.EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = rechnung.KundeId!.Value,
            Name = "Vorbefüllter Empfänger"
        };
        rechnung.AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = rechnung.FirmaProfilId!.Value,
            Name = "Vorbefüllter Absender"
        };
        rechnung.Kunde = null;
        rechnung.Absender = null;

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains(
            "Die Stammdaten des ausgewählten Kunden fehlen für den Rechnungsempfänger-Snapshot.",
            exception.Errors);
        Assert.Contains(
            "Die Stammdaten des ausgewählten Firmenprofils fehlen für den Rechnungsabsender-Snapshot.",
            exception.Errors);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task SpeichernAsync_ForCreate_LoadsLogoIntoSenderSnapshot()
    {
        byte[] geladeneBytes = [1, 2, 3, 4];
        var loader = new FakeLogoSnapshotLoader
        {
            Ergebnis = new LogoSnapshotDaten(geladeneBytes, "image/png")
        };
        var service = new RechnungService(new FakeRechnungRepository(), loader);
        var rechnung = CreateValidInvoice(id: null, nummer: string.Empty);

        await service.SpeichernAsync(rechnung);

        var snapshot = Assert.IsType<RechnungsAbsenderSnapshot>(rechnung.AbsenderSnapshot);
        Assert.Equal(1, loader.CallCount);
        Assert.Equal("logos/synthetisch.png", loader.LetzterPfad);
        Assert.Equal("image/png", snapshot.LogoMedientyp);
        Assert.Equal([1, 2, 3, 4], snapshot.LogoInhalt);

        geladeneBytes[0] = 9;
        var herausgegebeneBytes = snapshot.LogoInhalt!;
        herausgegebeneBytes[1] = 9;

        Assert.Equal([1, 2, 3, 4], snapshot.LogoInhalt);
    }

    [Fact]
    public async Task SpeichernAsync_WithChangedSenderProfile_LoadsNewLogo()
    {
        var loader = new FakeLogoSnapshotLoader
        {
            Ergebnis = new LogoSnapshotDaten([5, 6, 7], "image/webp")
        };
        var service = new RechnungService(new FakeRechnungRepository(), loader);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = 20,
            Name = "Historischer Absender",
            LogoInhalt = [1],
            LogoMedientyp = "image/png"
        };

        await service.SpeichernAsync(rechnung);

        Assert.Equal(1, loader.CallCount);
        Assert.Equal([5, 6, 7], rechnung.AbsenderSnapshot!.LogoInhalt);
        Assert.Equal("image/webp", rechnung.AbsenderSnapshot.LogoMedientyp);
    }

    [Fact]
    public async Task SpeichernAsync_WhenLogoLoaderFails_CreatesSnapshotWithoutLogo()
    {
        var loader = new FakeLogoSnapshotLoader
        {
            Fehler = new IOException("Synthetischer Logo-Ladefehler")
        };
        var service = new RechnungService(new FakeRechnungRepository(), loader);
        var rechnung = CreateValidInvoice(id: null, nummer: string.Empty);

        await service.SpeichernAsync(rechnung);

        Assert.Equal(1, loader.CallCount);
        Assert.NotNull(rechnung.AbsenderSnapshot);
        Assert.Null(rechnung.AbsenderSnapshot.LogoInhalt);
        Assert.Null(rechnung.AbsenderSnapshot.LogoMedientyp);
    }

    [Fact]
    public async Task SpeichernAsync_WithStableSenderSnapshot_DoesNotReloadLogo()
    {
        var loader = new FakeLogoSnapshotLoader
        {
            Ergebnis = new LogoSnapshotDaten([8, 9], "image/jpeg")
        };
        var service = new RechnungService(new FakeRechnungRepository(), loader);
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        var snapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = rechnung.FirmaProfilId!.Value,
            Name = "Historischer Absender",
            LogoInhalt = [1, 2],
            LogoMedientyp = "image/png"
        };
        rechnung.AbsenderSnapshot = snapshot;

        await service.SpeichernAsync(rechnung);

        Assert.Equal(0, loader.CallCount);
        Assert.Same(snapshot, rechnung.AbsenderSnapshot);
        Assert.Equal([1, 2], rechnung.AbsenderSnapshot.LogoInhalt);
        Assert.Equal("image/png", rechnung.AbsenderSnapshot.LogoMedientyp);
    }

    [Fact]
    public async Task SpeichernAsync_WithInvalidInvoice_CollectsValidationErrors()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);
        var rechnung = new Rechnung
        {
            Rechnungsdatum = default,
            Waehrung = "EU",
            Status = "unbekannt"
        };

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.SpeichernAsync(rechnung));

        Assert.Contains("Das Rechnungsdatum ist ungültig.", exception.Fehler);
        Assert.Contains("Ein Kunde ist erforderlich.", exception.Fehler);
        Assert.Contains("Ein Firmenprofil ist erforderlich.", exception.Fehler);
        Assert.Contains("Der Rechnungsstatus ist ungültig.", exception.Fehler);
        Assert.Contains("Die Währung muss aus genau drei Buchstaben bestehen.", exception.Fehler);
        Assert.Contains("Die Rechnung muss mindestens eine Position enthalten.", exception.Fehler);
    }

    [Fact]
    public async Task GetAllAsync_ForwardsStatusAndReturnsRepositoryResult()
    {
        var overview = new RechnungsUebersicht
        {
            Id = 42,
            Nummer = "RE-2026-0042",
            Status = RechnungsStatus.Offen
        };
        var repository = new FakeRechnungRepository
        {
            Overviews = [overview]
        };
        var service = new RechnungService(repository);

        var result = await service.GetAllAsync(RechnungsStatus.Offen);

        Assert.Same(repository.Overviews, result);
        Assert.Equal(RechnungsStatus.Offen, repository.LastStatusFilter);
    }

    [Fact]
    public async Task GetByIdAndDelete_DelegateToRepository()
    {
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var service = new RechnungService(repository);

        var loaded = await service.GetByIdAsync(42);
        await service.DeleteAsync(42);

        Assert.Same(rechnung, loaded);
        Assert.Equal(42, repository.LastLoadedId);
        Assert.Equal(42, repository.LastDeletedId);
    }

    [Fact]
    public async Task DeleteAsync_WithLinkedPdf_DeletesPdfBeforeDatabaseRecord()
    {
        var operationen = new List<string>();
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            "2026/rechnung-42.pdf",
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new FakeRechnungRepository
        {
            InvoiceToLoad = rechnung,
            Operationen = operationen
        };
        var pdfAblage = new FakePdfAblage { Operationen = operationen };
        var service = new RechnungService(repository, pdfAblage: pdfAblage);

        await service.DeleteAsync(42);

        Assert.Equal(
            ["pdf-loeschen:2026/rechnung-42.pdf", "rechnung-loeschen:42"],
            operationen);
        Assert.Equal(42, repository.LastDeletedId);
    }

    [Fact]
    public async Task DeleteAsync_WhenPdfDeletionFails_PreservesDatabaseRecord()
    {
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            "2026/rechnung-42.pdf",
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var erwarteterFehler = new IOException("Synthetischer PDF-Löschfehler");
        var pdfAblage = new FakePdfAblage { DeleteException = erwarteterFehler };
        var service = new RechnungService(repository, pdfAblage: pdfAblage);

        var fehler = await Assert.ThrowsAsync<IOException>(() => service.DeleteAsync(42));

        Assert.Same(erwarteterFehler, fehler);
        Assert.Null(repository.LastDeletedId);
    }

    [Fact]
    public async Task StatusAendernAsync_LoadsChangesAndUpdatesInvoice()
    {
        var rechnung = CreateValidInvoice(id: 42, nummer: "RE-2026-0042");
        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var service = new RechnungService(repository);

        var result = await service.StatusAendernAsync(42, RechnungsStatus.Bezahlt);

        Assert.Same(rechnung, result);
        Assert.Equal(RechnungsStatus.Bezahlt, rechnung.Status);
        Assert.Equal(42, repository.LastLoadedId);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task StatusAendernAsync_WithInvalidStatus_DoesNotLoadInvoice()
    {
        var repository = new FakeRechnungRepository();
        var service = new RechnungService(repository);

        var exception = await Assert.ThrowsAsync<RechnungValidationException>(
            () => service.StatusAendernAsync(42, "Bezahlt"));

        Assert.Contains("Der Rechnungsstatus ist ungültig.", exception.Errors);
        Assert.Null(repository.LastLoadedId);
    }

    private static Rechnung CreateValidInvoice(int? id, string nummer)
    {
        return new Rechnung
        {
            Id = id,
            Nummer = nummer,
            Rechnungsdatum = new DateTime(2026, 8, 3),
            Faeligkeitsdatum = new DateTime(2026, 8, 17),
            KundeId = 11,
            Kunde = new Kunde
            {
                Id = 11,
                Firmenname = "Synthetischer Kunde GmbH",
                Ansprechpartner = "Klara Kunde",
                Strasse = "Testweg 11",
                PLZ = "10115",
                Ort = "Berlin",
                Land = "DE",
                Email = "kunde@example.com",
                Telefon = "+49 30 111111",
                UstIdNr = "DE123456789"
            },
            FirmaProfilId = 21,
            Absender = new FirmaProfil
            {
                Id = 21,
                Name = "Synthetischer Absender GmbH",
                LogoPfad = "logos/synthetisch.png",
                Ansprechpartner = "Anton Absender",
                Strasse = "Prüfstraße 21",
                PLZ = "20095",
                Ort = "Hamburg",
                Land = "DE",
                Email = "absender@example.com",
                Telefon = "+49 30 123456",
                UstIdNr = "DE987654321",
                IBAN = "DE12500105170648489890",
                BIC = "INGDDEFFXXX"
            },
            Status = RechnungsStatus.Erstellt,
            Waehrung = "EUR",
            Positionen =
            [
                new RechnungsPosition
                {
                    Beschreibung = "Synthetische Dienstleistung",
                    Menge = 2m,
                    EinzelpreisNetto = 100m,
                    Steuersatz = 19m
                }
            ]
        };
    }

    private sealed class FakeLogoSnapshotLoader : ILogoSnapshotLoader
    {
        public LogoSnapshotDaten? Ergebnis { get; init; }
        public Exception? Fehler { get; init; }
        public int CallCount { get; private set; }
        public string? LetzterPfad { get; private set; }

        public LogoSnapshotDaten? Lade(string logoPfad)
        {
            CallCount++;
            LetzterPfad = logoPfad;

            if (Fehler is not null)
            {
                throw Fehler;
            }

            return Ergebnis;
        }
    }

    private sealed class FakePdfAblage : IRechnungsPdfAblage
    {
        public Exception? DeleteException { get; init; }
        public List<string>? Operationen { get; init; }

        public Task<string> SpeichereAsync(
            Rechnung rechnung,
            ReadOnlyMemory<byte> pdfInhalt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public bool Existiert(string relativerPfad) => false;

        public string LoeseVollstaendigenPfadAuf(string relativerPfad) => relativerPfad;

        public Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operationen?.Add($"pdf-loeschen:{relativerPfad}");
            return DeleteException is null
                ? Task.CompletedTask
                : Task.FromException(DeleteException);
        }
    }

    private sealed class FakeRechnungRepository : IRechnungRepository
    {
        public IReadOnlyList<RechnungsUebersicht> Overviews { get; init; } = [];
        public Rechnung? InvoiceToLoad { get; init; }
        public List<string>? Operationen { get; init; }
        public Rechnung? UpdatedInvoice { get; private set; }
        public DateTime? UpdatedGeaendertAm { get; private set; }
        public string? LastStatusFilter { get; private set; }
        public int? LastLoadedId { get; private set; }
        public int? LastDeletedId { get; private set; }
        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null)
        {
            LastStatusFilter = status;
            return Task.FromResult(Overviews);
        }

        public Task<Rechnung?> GetByIdAsync(int id)
        {
            LastLoadedId = id;
            return Task.FromResult(InvoiceToLoad);
        }

        public Task<Rechnung> CreateAsync(Rechnung rechnung)
        {
            CreateCallCount++;
            rechnung.Id ??= 1;
            if (string.IsNullOrWhiteSpace(rechnung.Nummer))
            {
                rechnung.Nummer = "RE-2026-0001";
            }

            return Task.FromResult(rechnung);
        }

        public Task UpdateAsync(Rechnung rechnung)
        {
            UpdateCallCount++;
            UpdatedInvoice = rechnung;
            UpdatedGeaendertAm = rechnung.GeaendertAm;
            return Task.CompletedTask;
        }

        public Task SetPdfVerknuepfungAsync(
            int id,
            RechnungsPdfVerknuepfung verknuepfung,
            DateTime erwartetGeaendertAm,
            RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            LastDeletedId = id;
            Operationen?.Add($"rechnung-loeschen:{id}");
            return Task.CompletedTask;
        }

        public Task DeleteIfUnchangedAsync(
            int id,
            DateTime erwartetGeaendertAm,
            RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
        {
            return DeleteAsync(id);
        }
    }
}
