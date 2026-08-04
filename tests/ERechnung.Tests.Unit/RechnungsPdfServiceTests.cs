using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class RechnungsPdfServiceTests
{
    private static readonly DateTime Rechnungsstand = new(
        2026,
        8,
        3,
        9,
        30,
        0,
        DateTimeKind.Utc);

    [Fact]
    public async Task ErzeugeAsync_WithValidInvoice_StoresAndLinksPdfForReadVersion()
    {
        var rechnung = CreateValidInvoice();
        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var generator = new FakePdfGenerator { Ergebnis = CreatePdfBytes() };
        var ablage = new FakePdfAblage { NeuerRelativerPfad = "rechnungen/42/v2.pdf" };
        var erzeugtAm = new DateTimeOffset(2026, 8, 3, 10, 15, 30, TimeSpan.Zero);
        var service = new RechnungsPdfService(
            repository,
            generator,
            ablage,
            new FixedTimeProvider(erzeugtAm));

        var result = await service.ErzeugeAsync(42);

        Assert.Same(rechnung, result);
        Assert.Same(rechnung, generator.LetzteRechnung);
        Assert.Equal(erzeugtAm, generator.LetzterErzeugungszeitpunkt);
        Assert.Same(rechnung, ablage.LetzteRechnung);
        Assert.Equal(CreatePdfBytes(), ablage.LetzterPdfInhalt);
        Assert.Equal(42, repository.LetztePdfRechnungsId);
        Assert.Equal(Rechnungsstand, repository.LetzterErwarteterRechnungsstand);
        Assert.Null(repository.LetzteErwartetePdfVerknuepfung);
        Assert.Equal(Rechnungsstand, rechnung.GeaendertAm);

        var verknuepfung = Assert.IsType<RechnungsPdfVerknuepfung>(rechnung.PdfVerknuepfung);
        Assert.Same(verknuepfung, repository.LetztePdfVerknuepfung);
        Assert.Equal("rechnungen/42/v2.pdf", verknuepfung.RelativerPfad);
        Assert.Equal(erzeugtAm.UtcDateTime, verknuepfung.ErstelltAm);
        Assert.Equal(DateTimeKind.Utc, verknuepfung.ErstelltAm.Kind);
        Assert.Equal(Rechnungsstand, verknuepfung.RechnungsstandAm);
        Assert.Empty(ablage.GeloeschtePfade);
    }

    [Fact]
    public async Task ErzeugeAsync_WithoutStoredInvoice_ThrowsKeyNotFoundException()
    {
        var repository = new FakeRechnungRepository();
        var generator = new FakePdfGenerator { Ergebnis = CreatePdfBytes() };
        var ablage = new FakePdfAblage();
        var service = new RechnungsPdfService(repository, generator, ablage);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ErzeugeAsync(42));

        Assert.Equal(0, generator.CallCount);
        Assert.Equal(0, ablage.SaveCallCount);
        Assert.Equal(0, repository.SetPdfCallCount);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("nummer")]
    [InlineData("empfaenger")]
    [InlineData("absender")]
    [InlineData("positionen")]
    public async Task ErzeugeAsync_WithInvalidStoredAggregate_RejectsBeforeGeneration(string fehler)
    {
        var rechnung = CreateValidInvoice();
        switch (fehler)
        {
            case "id":
                rechnung.Id = null;
                break;
            case "nummer":
                rechnung.Nummer = " ";
                break;
            case "empfaenger":
                rechnung.EmpfaengerSnapshot = null;
                break;
            case "absender":
                rechnung.AbsenderSnapshot = null;
                break;
            case "positionen":
                rechnung.Positionen = [];
                break;
        }

        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var generator = new FakePdfGenerator { Ergebnis = CreatePdfBytes() };
        var ablage = new FakePdfAblage();
        var service = new RechnungsPdfService(repository, generator, ablage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ErzeugeAsync(42));

        Assert.Equal(0, generator.CallCount);
        Assert.Equal(0, ablage.SaveCallCount);
        Assert.Equal(0, repository.SetPdfCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task ErzeugeAsync_WithTooShortGeneratorOutput_DoesNotStore(int laenge)
    {
        var repository = new FakeRechnungRepository { InvoiceToLoad = CreateValidInvoice() };
        var generator = new FakePdfGenerator { Ergebnis = new byte[laenge] };
        var ablage = new FakePdfAblage();
        var service = new RechnungsPdfService(repository, generator, ablage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ErzeugeAsync(42));

        Assert.Equal(0, ablage.SaveCallCount);
        Assert.Equal(0, repository.SetPdfCallCount);
    }

    [Fact]
    public async Task ErzeugeAsync_WhenStorageFails_DoesNotLinkPdf()
    {
        var rechnung = CreateValidInvoice();
        var repository = new FakeRechnungRepository { InvoiceToLoad = rechnung };
        var generator = new FakePdfGenerator { Ergebnis = CreatePdfBytes() };
        var erwarteterFehler = new IOException("Synthetischer Ablagefehler");
        var ablage = new FakePdfAblage { SaveException = erwarteterFehler };
        var service = new RechnungsPdfService(repository, generator, ablage);

        var fehler = await Assert.ThrowsAsync<IOException>(() => service.ErzeugeAsync(42));

        Assert.Same(erwarteterFehler, fehler);
        Assert.Equal(0, repository.SetPdfCallCount);
        Assert.Null(rechnung.PdfVerknuepfung);
        Assert.Empty(ablage.GeloeschtePfade);
    }

    [Fact]
    public async Task ErzeugeAsync_WhenConcurrencyLinkFails_DeletesNewFile()
    {
        var alteVerknuepfung = new RechnungsPdfVerknuepfung(
            "rechnungen/42/alt.pdf",
            new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc),
            Rechnungsstand.AddDays(-1));
        var rechnung = CreateValidInvoice();
        rechnung.PdfVerknuepfung = alteVerknuepfung;
        var erwarteterFehler = new InvalidOperationException("Synthetischer Concurrency-Konflikt");
        var repository = new FakeRechnungRepository
        {
            InvoiceToLoad = rechnung,
            SetPdfException = erwarteterFehler
        };
        var generator = new FakePdfGenerator { Ergebnis = CreatePdfBytes() };
        var ablage = new FakePdfAblage { NeuerRelativerPfad = "rechnungen/42/neu.pdf" };
        var service = new RechnungsPdfService(repository, generator, ablage);

        var fehler = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ErzeugeAsync(42));

        Assert.Same(erwarteterFehler, fehler);
        Assert.Equal(["rechnungen/42/neu.pdf"], ablage.GeloeschtePfade);
        Assert.Same(alteVerknuepfung, rechnung.PdfVerknuepfung);
        Assert.Equal(Rechnungsstand, repository.LetzterErwarteterRechnungsstand);
        Assert.Same(alteVerknuepfung, repository.LetzteErwartetePdfVerknuepfung);
    }

    [Fact]
    public async Task ErzeugeAsync_AfterSuccessfulLink_DeletesPreviousDifferentPdf()
    {
        var operationen = new List<string>();
        var alteVerknuepfung = new RechnungsPdfVerknuepfung(
            "rechnungen/42/alt.pdf",
            new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc),
            Rechnungsstand.AddDays(-1));
        var rechnung = CreateValidInvoice();
        rechnung.PdfVerknuepfung = alteVerknuepfung;
        var repository = new FakeRechnungRepository
        {
            InvoiceToLoad = rechnung,
            Operationen = operationen
        };
        var ablage = new FakePdfAblage
        {
            NeuerRelativerPfad = "rechnungen/42/neu.pdf",
            Operationen = operationen
        };
        var service = new RechnungsPdfService(
            repository,
            new FakePdfGenerator { Ergebnis = CreatePdfBytes() },
            ablage);

        await service.ErzeugeAsync(42);

        Assert.Equal(
            [
                "speichern:rechnungen/42/neu.pdf",
                "verknuepfen:rechnungen/42/neu.pdf",
                "loeschen:rechnungen/42/alt.pdf"
            ],
            operationen);
        Assert.Equal(["rechnungen/42/alt.pdf"], ablage.GeloeschtePfade);
        Assert.Equal("rechnungen/42/neu.pdf", rechnung.PdfVerknuepfung!.RelativerPfad);
    }

    private static Rechnung CreateValidInvoice()
    {
        return new Rechnung
        {
            Id = 42,
            Nummer = "RE-2026-0042",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            GeaendertAm = Rechnungsstand,
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = 11,
                Name = "Synthetischer Empfänger"
            },
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = 21,
                Name = "Synthetischer Absender"
            },
            Positionen =
            [
                new RechnungsPosition
                {
                    Beschreibung = "Synthetische Leistung",
                    Menge = 1m,
                    EinzelpreisNetto = 100m,
                    Steuersatz = 19m
                }
            ]
        };
    }

    private static byte[] CreatePdfBytes()
    {
        return [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakePdfGenerator : IRechnungsPdfGenerator
    {
        public required byte[] Ergebnis { get; init; }
        public int CallCount { get; private set; }
        public Rechnung? LetzteRechnung { get; private set; }
        public DateTimeOffset? LetzterErzeugungszeitpunkt { get; private set; }

        public byte[] Erzeuge(Rechnung rechnung, DateTimeOffset erzeugtAm)
        {
            CallCount++;
            LetzteRechnung = rechnung;
            LetzterErzeugungszeitpunkt = erzeugtAm;
            return Ergebnis;
        }
    }

    private sealed class FakePdfAblage : IRechnungsPdfAblage
    {
        public string NeuerRelativerPfad { get; init; } = "rechnungen/42/v1.pdf";
        public Exception? SaveException { get; init; }
        public Exception? DeleteException { get; init; }
        public List<string>? Operationen { get; init; }
        public int SaveCallCount { get; private set; }
        public Rechnung? LetzteRechnung { get; private set; }
        public byte[]? LetzterPdfInhalt { get; private set; }
        public List<string> GeloeschtePfade { get; } = [];

        public Task<string> SpeichereAsync(
            Rechnung rechnung,
            ReadOnlyMemory<byte> pdfInhalt,
            CancellationToken cancellationToken)
        {
            SaveCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            if (SaveException is not null)
            {
                return Task.FromException<string>(SaveException);
            }

            LetzteRechnung = rechnung;
            LetzterPdfInhalt = pdfInhalt.ToArray();
            Operationen?.Add($"speichern:{NeuerRelativerPfad}");
            return Task.FromResult(NeuerRelativerPfad);
        }

        public bool Existiert(string relativerPfad)
        {
            return string.Equals(relativerPfad, NeuerRelativerPfad, StringComparison.Ordinal);
        }

        public string LoeseVollstaendigenPfadAuf(string relativerPfad)
        {
            if (relativerPfad.Contains("..", StringComparison.Ordinal))
            {
                throw new ArgumentException("Synthetisch unsicherer Pfad.", nameof(relativerPfad));
            }

            return $"synthetische-ablage/{relativerPfad}";
        }

        public Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GeloeschtePfade.Add(relativerPfad);
            Operationen?.Add($"loeschen:{relativerPfad}");
            return DeleteException is null
                ? Task.CompletedTask
                : Task.FromException(DeleteException);
        }
    }

    private sealed class FakeRechnungRepository : IRechnungRepository
    {
        public Rechnung? InvoiceToLoad { get; init; }
        public Exception? SetPdfException { get; init; }
        public List<string>? Operationen { get; init; }
        public int SetPdfCallCount { get; private set; }
        public int? LetztePdfRechnungsId { get; private set; }
        public RechnungsPdfVerknuepfung? LetztePdfVerknuepfung { get; private set; }
        public DateTime? LetzterErwarteterRechnungsstand { get; private set; }
        public RechnungsPdfVerknuepfung? LetzteErwartetePdfVerknuepfung { get; private set; }

        public Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null)
        {
            return Task.FromResult<IReadOnlyList<RechnungsUebersicht>>([]);
        }

        public Task<Rechnung?> GetByIdAsync(int id)
        {
            return Task.FromResult(InvoiceToLoad);
        }

        public Task<Rechnung> CreateAsync(Rechnung rechnung)
        {
            return Task.FromResult(rechnung);
        }

        public Task UpdateAsync(Rechnung rechnung)
        {
            return Task.CompletedTask;
        }

        public Task SetPdfVerknuepfungAsync(
            int id,
            RechnungsPdfVerknuepfung verknuepfung,
            DateTime erwartetGeaendertAm,
            RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
        {
            SetPdfCallCount++;
            LetztePdfRechnungsId = id;
            LetztePdfVerknuepfung = verknuepfung;
            LetzterErwarteterRechnungsstand = erwartetGeaendertAm;
            LetzteErwartetePdfVerknuepfung = erwartetePdfVerknuepfung;
            Operationen?.Add($"verknuepfen:{verknuepfung.RelativerPfad}");

            return SetPdfException is null
                ? Task.CompletedTask
                : Task.FromException(SetPdfException);
        }

        public Task DeleteAsync(int id)
        {
            return Task.CompletedTask;
        }

        public Task DeleteIfUnchangedAsync(
            int id,
            DateTime erwartetGeaendertAm,
            RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung)
        {
            return Task.CompletedTask;
        }
    }
}
