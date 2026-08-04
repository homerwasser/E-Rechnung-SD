using ERechnung.App.Services;
using ERechnung.App.ViewModels;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.App;

public sealed class M4ViewModelTests
{
    [Fact]
    public async Task NeuerRechnungsentwurf_SetztLeistungsdatumAufHeute()
    {
        var viewModel = ErstelleEditor(new StubRechnungRepository());

        await viewModel.InitialisiereNeuAsync();

        Assert.Equal(DateTime.Today, viewModel.Leistungsdatum);
        Assert.Equal(DateTime.Today, viewModel.RechnungsEntwurf.Leistungsdatum);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task Leistungsdatum_WirdBeimBearbeitenUebernommenGespeichertUndSetztDirtyState()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var repository = new StubRechnungRepository([rechnung]);
        var viewModel = ErstelleEditor(repository);

        await viewModel.InitialisiereBearbeitungAsync(rechnung.Id!.Value);

        Assert.Equal(new DateTime(2026, 7, 31), viewModel.Leistungsdatum);
        Assert.False(viewModel.HatUngespeicherteAenderungen);

        var neuesLeistungsdatum = new DateTime(2026, 8, 2);
        viewModel.Leistungsdatum = neuesLeistungsdatum;

        Assert.True(viewModel.HatUngespeicherteAenderungen);

        await viewModel.SpeichernAsync();

        Assert.Equal(1, repository.UpdateAufrufe);
        Assert.Equal(neuesLeistungsdatum, rechnung.Leistungsdatum);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
    }

    [Theory]
    [InlineData("kein", "Kein PDF", false, false)]
    [InlineData("veraltet", "Veraltet", true, false)]
    [InlineData("fehlt", "Datei fehlt", false, false)]
    [InlineData("aktuell", "Aktuell", true, true)]
    public void RechnungsListenEintrag_ErmitteltExaktenPdfStatusUndCommandFaehigkeit(
        string fall,
        string erwarteterStatus,
        bool kannOeffnen,
        bool kannEmail)
    {
        using var pdfAblage = new StubPdfAblage();
        var stand = new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc);
        var model = new RechnungsUebersicht
        {
            Id = 10,
            Nummer = "2026-001",
            GeaendertAm = stand
        };

        if (fall != "kein")
        {
            model.PdfRelativerPfad = "2026/rechnung-10.pdf";
            model.PdfRechnungsstandAm = fall == "veraltet" ? stand.AddMinutes(-1) : stand;
        }

        if (fall is "veraltet" or "aktuell")
        {
            pdfAblage.FuegePdfHinzu(model.PdfRelativerPfad!);
        }

        var eintrag = new RechnungsListenEintragViewModel(model, pdfAblage);

        Assert.Equal(erwarteterStatus, eintrag.PdfStatus);
        Assert.True(eintrag.KannPdfErstellen);
        Assert.Equal(kannOeffnen, eintrag.KannPdfOeffnen);
        Assert.Equal(kannOeffnen, eintrag.KannImExplorerAnzeigen);
        Assert.Equal(kannEmail, eintrag.KannEmailEntwurfOeffnen);
    }

    [Fact]
    public async Task Loeschen_MitPdfVerknuepfung_ZeigtWarnungDassPdfAuchGeloeschtWird()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var relativerPfad = "2026/rechnung-10.pdf";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            relativerPfad,
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        pdfAblage.FuegePdfHinzu(relativerPfad);
        var dialog = new StubDialogService { Bestaetigen = false };
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            dialog,
            pdfAblage: pdfAblage);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        ((System.Windows.Input.ICommand)viewModel.LoeschenCommand).Execute(null);
        await WarteBisAsync(() => dialog.Bestaetigungen.Count > 0);

        var bestaetigung = Assert.Single(dialog.Bestaetigungen);
        Assert.Contains("PDF", bestaetigung.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gelöscht", bestaetigung.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(viewModel.Rechnungen);
    }

    [Fact]
    public async Task Loeschen_OhnePdfVerknueftung_ZeigtKeinePdfWarnung()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        var dialog = new StubDialogService { Bestaetigen = false };
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            dialog,
            pdfAblage: pdfAblage);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        ((System.Windows.Input.ICommand)viewModel.LoeschenCommand).Execute(null);
        await WarteBisAsync(() => dialog.Bestaetigungen.Count > 0);

        var bestaetigung = Assert.Single(dialog.Bestaetigungen);
        Assert.DoesNotContain("PDF", bestaetigung.Message);
        Assert.Single(viewModel.Rechnungen);
    }

    [Fact]
    public async Task PdfCommand_RespektiertAuswahlUndBusyErzeugtPdfUndErhaeltAuswahl()
    {
        var rechnung = TestData.GespeicherteRechnung(status: RechnungsStatus.Versendet);
        var repository = new StubRechnungRepository([rechnung]);
        var generator = new StubPdfGenerator();
        using var pdfAblage = new BlockierendePdfAblage();
        var dialog = new StubDialogService();
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            dialog,
            pdfAblage: pdfAblage,
            pdfGenerator: generator);
        await viewModel.InitialisierenAsync();

        Assert.False(viewModel.BearbeitenCommand.CanExecute(null));
        Assert.False(viewModel.LoeschenCommand.CanExecute(null));
        Assert.False(viewModel.StatusAendernCommand.CanExecute(null));
        Assert.False(viewModel.PdfErstellenAktualisierenCommand.CanExecute(null));
        Assert.False(viewModel.PdfOeffnenCommand.CanExecute(null));
        Assert.False(viewModel.ImExplorerAnzeigenCommand.CanExecute(null));
        Assert.False(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));

        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        Assert.True(viewModel.PdfErstellenAktualisierenCommand.CanExecute(null));
        Assert.False(viewModel.PdfOeffnenCommand.CanExecute(null));
        Assert.False(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));

        viewModel.PdfErstellenAktualisierenCommand.Execute(null);
        await pdfAblage.SpeichernBegonnen.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(viewModel.IstBeschaeftigt);
        Assert.False(viewModel.NeuCommand.CanExecute(null));
        Assert.False(viewModel.FilterAnwendenCommand.CanExecute(null));
        Assert.False(viewModel.BearbeitenCommand.CanExecute(null));
        Assert.False(viewModel.LoeschenCommand.CanExecute(null));
        Assert.False(viewModel.StatusAendernCommand.CanExecute(null));
        Assert.False(viewModel.PdfErstellenAktualisierenCommand.CanExecute(null));
        Assert.False(viewModel.PdfOeffnenCommand.CanExecute(null));
        Assert.False(viewModel.ImExplorerAnzeigenCommand.CanExecute(null));
        Assert.False(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));

        pdfAblage.GibSpeichernFrei();
        await WarteBisAsync(() => !viewModel.IstBeschaeftigt);

        Assert.Equal(1, generator.Aufrufe);
        Assert.Equal(1, repository.SetPdfAufrufe);
        Assert.Equal(1, pdfAblage.SpeicherAufrufe);
        Assert.NotNull(viewModel.AusgewaehlteRechnung);
        Assert.Equal(rechnung.Id, viewModel.AusgewaehlteRechnung.Id);
        Assert.Equal("Aktuell", viewModel.AusgewaehlteRechnung.PdfStatus);
        Assert.True(viewModel.PdfOeffnenCommand.CanExecute(null));
        Assert.True(viewModel.ImExplorerAnzeigenCommand.CanExecute(null));
        Assert.True(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));
        Assert.Contains("erstellt", viewModel.Statusmeldung, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RechnungsStatus.Versendet, rechnung.Status);
        Assert.Equal(0, repository.UpdateAufrufe);
        Assert.Empty(dialog.Fehler);
    }

    [Fact]
    public async Task VeraltetePdf_KannGeoeffnetWerdenAberNichtPerEmailVerwendetWerden()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var relativerPfad = "2026/rechnung-10.pdf";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            relativerPfad,
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm.AddMinutes(-1));
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        pdfAblage.FuegePdfHinzu(relativerPfad);
        var dateiOeffner = new StubDateiOeffner();
        var emailService = new StubEmailEntwurfService();
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            pdfAblage: pdfAblage,
            dateiOeffner: dateiOeffner,
            emailService: emailService);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        Assert.Equal("Veraltet", viewModel.AusgewaehlteRechnung.PdfStatus);
        Assert.True(viewModel.PdfOeffnenCommand.CanExecute(null));
        Assert.False(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));

        viewModel.PdfOeffnen();
        await viewModel.EmailEntwurfOeffnenAsync();

        Assert.Single(dateiOeffner.GeoeffneteDateien);
        Assert.Empty(emailService.Entwuerfe);
        Assert.Equal(0, repository.UpdateAufrufe);
    }

    [Fact]
    public async Task EmailEntwurf_PrueftNachDemListenladenErneutObPdfAktuellIst()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var relativerPfad = "2026/rechnung-10.pdf";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            relativerPfad,
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        pdfAblage.FuegePdfHinzu(relativerPfad);
        var emailService = new StubEmailEntwurfService();
        var dialog = new StubDialogService();
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            dialog,
            pdfAblage,
            emailService: emailService);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);
        Assert.True(viewModel.EmailEntwurfOeffnenCommand.CanExecute(null));

        rechnung.GeaendertAm = rechnung.GeaendertAm.AddMinutes(1);
        rechnung.EmpfaengerSnapshot!.Email = "anderer-empfaenger@example.test";
        await viewModel.EmailEntwurfOeffnenAsync();

        Assert.Empty(emailService.Entwuerfe);
        var fehler = Assert.Single(dialog.Fehler);
        Assert.Contains("PDF-Erstellung geändert", fehler.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("erneut", fehler.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmailEntwurf_VerwendetDieFrischGeladenePdfVerknuepfung()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var alterPfad = "2026/rechnung-10-alt.pdf";
        var neuerPfad = "2026/rechnung-10-neu.pdf";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            alterPfad,
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        pdfAblage.FuegePdfHinzu(alterPfad);
        pdfAblage.FuegePdfHinzu(neuerPfad);
        var emailService = new StubEmailEntwurfService();
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            pdfAblage: pdfAblage,
            emailService: emailService);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            neuerPfad,
            new DateTime(2026, 8, 3, 10, 1, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        await viewModel.EmailEntwurfOeffnenAsync();

        var entwurf = Assert.Single(emailService.Entwuerfe);
        Assert.Equal(pdfAblage.LoeseVollstaendigenPfadAuf(neuerPfad), entwurf.AnhangPfad);
    }

    [Fact]
    public async Task AktuellePdf_OeffnetDateiUndExplorerUndFallbackZeigtInfoOhneStatusaenderung()
    {
        var rechnung = TestData.GespeicherteRechnung(status: RechnungsStatus.Bezahlt);
        var relativerPfad = "2026/rechnung-10.pdf";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            relativerPfad,
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            rechnung.GeaendertAm);
        var repository = new StubRechnungRepository([rechnung]);
        using var pdfAblage = new StubPdfAblage();
        pdfAblage.FuegePdfHinzu(relativerPfad);
        var dateiOeffner = new StubDateiOeffner();
        var emailService = new StubEmailEntwurfService
        {
            Ergebnis = new EmailEntwurfErgebnis(
                EmailEntwurfErgebnisStatus.OhneAnhangAngefordert)
        };
        var dialog = new StubDialogService();
        var viewModel = TestViewModelFactory.ErstelleUebersicht(
            repository,
            dialog,
            pdfAblage,
            dateiOeffner: dateiOeffner,
            emailService: emailService);
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);

        viewModel.PdfOeffnen();
        viewModel.ImExplorerAnzeigen();
        await viewModel.EmailEntwurfOeffnenAsync();

        var pdfPfad = pdfAblage.LoeseVollstaendigenPfadAuf(relativerPfad);
        Assert.Equal([pdfPfad], dateiOeffner.GeoeffneteDateien);
        Assert.Equal([pdfPfad, pdfPfad], dateiOeffner.ImExplorerAngezeigteDateien);
        var entwurf = Assert.Single(emailService.Entwuerfe);
        Assert.Equal(rechnung.EmpfaengerSnapshot!.Email, entwurf.Empfaenger);
        var information = Assert.Single(dialog.Informationen);
        Assert.Contains("ohne Anhang", information.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manuell", information.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Explorer", information.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RechnungsStatus.Bezahlt, rechnung.Status);
        Assert.Equal(0, repository.UpdateAufrufe);
        Assert.Empty(dialog.Fehler);
    }

    private static RechnungsEditorViewModel ErstelleEditor(StubRechnungRepository repository)
    {
        return new RechnungsEditorViewModel(
            new StubRepository<Kunde>([TestData.Kunde()]),
            new StubRepository<FirmaProfil>([TestData.FirmaProfil()]),
            new RechnungService(repository),
            new StubDialogService());
    }

    private static async Task WarteBisAsync(Func<bool> bedingung)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!bedingung())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class BlockierendePdfAblage : IRechnungsPdfAblage, IDisposable
    {
        private readonly StubPdfAblage _inner = new();
        private readonly TaskCompletionSource _speichernFreigegeben = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SpeichernBegonnen { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public int SpeicherAufrufe => _inner.SpeicherAufrufe;

        public async Task<string> SpeichereAsync(
            Rechnung rechnung,
            ReadOnlyMemory<byte> pdfInhalt,
            CancellationToken cancellationToken)
        {
            SpeichernBegonnen.TrySetResult();
            await _speichernFreigegeben.Task.WaitAsync(cancellationToken);
            return await _inner.SpeichereAsync(rechnung, pdfInhalt, cancellationToken);
        }

        public bool Existiert(string relativerPfad) => _inner.Existiert(relativerPfad);

        public string LoeseVollstaendigenPfadAuf(string relativerPfad) =>
            _inner.LoeseVollstaendigenPfadAuf(relativerPfad);

        public Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken) =>
            _inner.LoescheAsync(relativerPfad, cancellationToken);

        public void GibSpeichernFrei() => _speichernFreigegeben.TrySetResult();

        public void Dispose() => _inner.Dispose();
    }
}
