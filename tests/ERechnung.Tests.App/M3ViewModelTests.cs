using System.Globalization;
using ERechnung.App.Converters;
using ERechnung.App.ViewModels;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.App;

public sealed class M3ViewModelTests
{
    [Theory]
    [InlineData("de-DE", "123,456")]
    [InlineData("en-US", "123.456")]
    public void DecimalInputConverter_RoundtripBleibtKulturabhaengigUndVerlustfrei(
        string kulturName,
        string erwarteterText)
    {
        var converter = new DecimalInputConverter();
        var kultur = CultureInfo.GetCultureInfo(kulturName);
        const decimal wert = 123.456m;

        var text = Assert.IsType<string>(
            converter.Convert(wert, typeof(string), null!, kultur));
        var roundtrip = Assert.IsType<decimal>(
            converter.ConvertBack(text, typeof(decimal), null!, kultur));

        Assert.Equal(erwarteterText, text);
        Assert.Equal(wert, roundtrip);
    }

    [Fact]
    public async Task Positionsaenderung_AktualisiertLiveSummen()
    {
        var (viewModel, _) = ErstelleEditor();
        await viewModel.InitialisiereNeuAsync();

        var position = Assert.Single(viewModel.Positionen);
        position.Beschreibung = "Beratung";
        position.Menge = 2.5m;
        position.EinzelpreisNetto = 10m;
        position.Steuersatz = 19m;

        Assert.Equal(25m, position.Positionsnetto);
        Assert.Equal(25m, viewModel.GesamtbetragNetto);
        Assert.Equal(4.75m, viewModel.UmsatzsteuerBetrag);
        Assert.Equal(29.75m, viewModel.GesamtbetragBrutto);
    }

    [Fact]
    public async Task Positionen_KoennenHinzugefuegtUndBisAufEineEntferntWerden()
    {
        var (viewModel, _) = ErstelleEditor();
        await viewModel.InitialisiereNeuAsync();
        var erstePosition = Assert.Single(viewModel.Positionen);

        viewModel.PositionHinzufuegen();
        Assert.Equal(2, viewModel.Positionen.Count);

        viewModel.PositionEntfernen(erstePosition);
        Assert.Single(viewModel.Positionen);
        viewModel.PositionEntfernen(viewModel.Positionen[0]);
        Assert.Single(viewModel.Positionen);
    }

    [Fact]
    public async Task NeuerEditor_LaedtStammdatenUndSetztDefaults()
    {
        var (viewModel, _) = ErstelleEditor();

        await viewModel.InitialisiereNeuAsync();

        Assert.Single(viewModel.Kunden);
        Assert.Single(viewModel.Firmenprofile);
        Assert.NotNull(viewModel.AusgewaehlterKunde);
        Assert.NotNull(viewModel.AusgewaehltesFirmaProfil);
        Assert.Equal(DateTime.Today, viewModel.Rechnungsdatum);
        Assert.Equal(DateTime.Today.AddDays(14), viewModel.Faelligkeitsdatum);
        Assert.Equal(RechnungsStatus.Erstellt, viewModel.Status);
        Assert.Equal("EUR", viewModel.RechnungsEntwurf.Waehrung);
        Assert.Single(viewModel.Positionen);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task UngueltigesSpeichern_BehaeltEntwurfUndZeigtAlleFehler()
    {
        var (viewModel, repository) = ErstelleEditor();
        await viewModel.InitialisiereNeuAsync();
        var entwurf = viewModel.RechnungsEntwurf;
        viewModel.Positionen[0].Menge = 0m;

        await viewModel.SpeichernAsync();

        Assert.Same(entwurf, viewModel.RechnungsEntwurf);
        Assert.Single(viewModel.Positionen);
        Assert.Contains("Beschreibung ist erforderlich", viewModel.Fehlermeldung);
        Assert.Contains("Menge muss größer als 0", viewModel.Fehlermeldung);
        Assert.Equal(0, repository.CreateAufrufe);
    }

    [Fact]
    public async Task GueltigesSpeichern_DelegiertUndMeldetErfolg()
    {
        var (viewModel, repository) = ErstelleEditor();
        await viewModel.InitialisiereNeuAsync();
        viewModel.Positionen[0].Beschreibung = "Entwicklung";
        viewModel.Positionen[0].EinzelpreisNetto = 120m;
        Rechnung? gemeldet = null;
        viewModel.RechnungGespeichertAsync = rechnung =>
        {
            gemeldet = rechnung;
            return Task.CompletedTask;
        };

        await viewModel.SpeichernAsync();

        Assert.Equal(1, repository.CreateAufrufe);
        Assert.Same(viewModel.RechnungsEntwurf, gemeldet);
        Assert.NotNull(gemeldet!.Id);
        Assert.Empty(viewModel.Fehlermeldung);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task EditorAenderungen_SetzenDirtyState()
    {
        var dialog = new StubDialogService();
        var (viewModel, _) = ErstelleEditor(dialog);
        await viewModel.InitialisiereNeuAsync();

        Assert.False(viewModel.HatUngespeicherteAenderungen);

        viewModel.Titel = "Geänderter Titel";
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.True(viewModel.BestaetigeVerwerfen());

        viewModel.AusgewaehlterKunde = TestData.Kunde(42);
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.True(viewModel.BestaetigeVerwerfen());

        viewModel.Positionen[0].Beschreibung = "Beratung";
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.True(viewModel.BestaetigeVerwerfen());

        viewModel.PositionHinzufuegen();
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.True(viewModel.BestaetigeVerwerfen());

        viewModel.PositionEntfernen(viewModel.Positionen[0]);
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.True(viewModel.BestaetigeVerwerfen());

        viewModel.RegistriereEingabefehler(hinzugefuegt: true);
        Assert.True(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task Abbrechen_BeruecksichtigtVerwerfentscheidung()
    {
        var dialog = new StubDialogService { Bestaetigen = false };
        var (viewModel, _) = ErstelleEditor(dialog);
        var abgebrochen = 0;
        viewModel.BearbeitungAbgebrochenAsync = () =>
        {
            abgebrochen++;
            return Task.CompletedTask;
        };
        await viewModel.InitialisiereNeuAsync();

        Assert.True(viewModel.BestaetigeVerwerfen());
        Assert.Empty(dialog.Bestaetigungen);

        viewModel.Titel = "Ungespeichert";
        await viewModel.AbbrechenAsync();

        Assert.Equal(0, abgebrochen);
        Assert.True(viewModel.HatUngespeicherteAenderungen);
        Assert.Single(dialog.Bestaetigungen);

        dialog.Bestaetigen = true;
        await viewModel.AbbrechenAsync();

        Assert.Equal(1, abgebrochen);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
        Assert.Equal(2, dialog.Bestaetigungen.Count);
    }

    [Fact]
    public async Task LeeresRechnungsdatum_VerhindertPersistenzUndVerwirftAltenWert()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var repository = new StubRechnungRepository([rechnung]);
        var (viewModel, _) = ErstelleEditor(repository: repository);
        await viewModel.InitialisiereBearbeitungAsync(rechnung.Id!.Value);

        Assert.Equal(new DateTime(2026, 8, 1), viewModel.Rechnungsdatum);
        viewModel.Rechnungsdatum = null;

        await viewModel.SpeichernAsync();

        Assert.Equal(0, repository.UpdateAufrufe);
        Assert.Equal(default, viewModel.RechnungsEntwurf.Rechnungsdatum);
        Assert.Contains("Rechnungsdatum ist ungültig", viewModel.Fehlermeldung);
        Assert.True(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task Bearbeitungsinitialisierung_ZeigtHistorischeSnapshotNamenUndIstClean()
    {
        var rechnung = TestData.GespeicherteRechnung();
        rechnung.EmpfaengerSnapshot!.Name = "Historischer Empfänger";
        rechnung.AbsenderSnapshot!.Name = "Historischer Absender";
        var repository = new StubRechnungRepository([rechnung]);
        var (viewModel, _) = ErstelleEditor(repository: repository);

        await viewModel.InitialisiereBearbeitungAsync(rechnung.Id!.Value);

        Assert.True(viewModel.HatSnapshotHinweis);
        Assert.Contains("Historischer Empfänger", viewModel.SnapshotHinweis);
        Assert.Contains("Historischer Absender", viewModel.SnapshotHinweis);
        Assert.Contains("gleicher ID", viewModel.SnapshotHinweis);
        Assert.False(viewModel.HatUngespeicherteAenderungen);
    }

    [Fact]
    public async Task Statusfilter_VerwendetKanonischenWert()
    {
        var repository = new StubRechnungRepository(
            [TestData.GespeicherteRechnung(status: RechnungsStatus.Bezahlt)]);
        var viewModel = new RechnungsUebersichtViewModel(
            new RechnungService(repository),
            new StubDialogService());
        viewModel.AusgewaehlterFilter = Assert.Single(
            viewModel.FilterOptionen,
            option => option.Anzeigename == "Bezahlt");

        await viewModel.LadeRechnungenAsync();

        Assert.Equal(RechnungsStatus.Bezahlt, repository.LetzterStatusFilter);
        Assert.Single(viewModel.Rechnungen);
    }

    [Fact]
    public async Task Statusaenderung_AktualisiertListe()
    {
        var rechnung = TestData.GespeicherteRechnung();
        var repository = new StubRechnungRepository([rechnung]);
        var viewModel = new RechnungsUebersichtViewModel(
            new RechnungService(repository),
            new StubDialogService());
        await viewModel.InitialisierenAsync();
        viewModel.AusgewaehlteRechnung = Assert.Single(viewModel.Rechnungen);
        viewModel.AusgewaehlterNeuerStatus = Assert.Single(
            viewModel.StatusOptionen,
            option => option.Wert == RechnungsStatus.Bezahlt);

        await viewModel.StatusAendernAsync();

        Assert.Equal(RechnungsStatus.Bezahlt, rechnung.Status);
        Assert.Equal("Bezahlt", Assert.Single(viewModel.Rechnungen).StatusAnzeige);
        Assert.Equal(1, repository.UpdateAufrufe);
    }

    [Fact]
    public async Task MainWindowNavigation_StartetInUebersichtUndOeffnetNeuenEntwurf()
    {
        var kundeRepository = new StubRepository<Kunde>([TestData.Kunde()]);
        var profilRepository = new StubRepository<FirmaProfil>([TestData.FirmaProfil()]);
        var rechnungRepository = new StubRechnungRepository();
        var service = new RechnungService(rechnungRepository);
        var dialog = new StubDialogService();
        var uebersicht = new RechnungsUebersichtViewModel(service, dialog);
        var editor = new RechnungsEditorViewModel(
            kundeRepository,
            profilRepository,
            service,
            dialog);
        var main = new MainWindowViewModel(
            uebersicht,
            editor,
            new KundenViewModel(kundeRepository, dialog),
            new FirmenprofileViewModel(profilRepository, dialog),
            dialog);

        Assert.Same(uebersicht, main.CurrentViewModel);
        await main.InitialisierenAsync();
        await main.NeueRechnungOeffnenAsync();

        Assert.Same(editor, main.CurrentViewModel);
        Assert.Null(editor.RechnungsEntwurf.Id);
        Assert.Single(editor.Positionen);
        Assert.Empty(dialog.Fehler);
    }

    [Fact]
    public async Task ErneuteNeueRechnung_BeiAbgelehntemVerwerfen_BehaeltEntwurf()
    {
        var (main, editor, _, _, _, dialog) = ErstelleMainWindow();
        await main.NeueRechnungOeffnenAsync();
        var entwurf = editor.RechnungsEntwurf;
        editor.Titel = "Diesen Entwurf behalten";
        dialog.Bestaetigen = false;

        await main.NeueRechnungOeffnenAsync();

        Assert.Same(editor, main.CurrentViewModel);
        Assert.Same(entwurf, editor.RechnungsEntwurf);
        Assert.Equal("Diesen Entwurf behalten", editor.Titel);
        Assert.True(editor.HatUngespeicherteAenderungen);
        Assert.Single(dialog.Bestaetigungen);
    }

    [Fact]
    public async Task Navigation_BeiBestaetigtemVerwerfen_WechseltAnsichtUndSetztClean()
    {
        var (main, editor, _, kunden, _, dialog) = ErstelleMainWindow();
        await main.NeueRechnungOeffnenAsync();
        editor.Titel = "Wird verworfen";

        await main.KundenOeffnenAsync();

        Assert.Same(kunden, main.CurrentViewModel);
        Assert.False(editor.HatUngespeicherteAenderungen);
        Assert.Single(dialog.Bestaetigungen);
    }

    [Fact]
    public async Task ErfolgreichesSpeichern_NavigiertOhneVerwerfrueckfrage()
    {
        var (main, editor, uebersicht, _, repository, dialog) = ErstelleMainWindow();
        await main.NeueRechnungOeffnenAsync();
        editor.Positionen[0].Beschreibung = "Entwicklung";
        editor.Positionen[0].EinzelpreisNetto = 120m;
        Assert.True(editor.HatUngespeicherteAenderungen);

        await editor.SpeichernAsync();

        Assert.Equal(1, repository.CreateAufrufe);
        Assert.Same(uebersicht, main.CurrentViewModel);
        Assert.False(editor.HatUngespeicherteAenderungen);
        Assert.Empty(dialog.Bestaetigungen);
    }

    [Fact]
    public async Task Fensterschliessen_VerwendetVerwerfentscheidungDesEditors()
    {
        var (main, editor, _, _, _, dialog) = ErstelleMainWindow();
        await main.NeueRechnungOeffnenAsync();
        editor.Bemerkung = "Ungespeichert";
        dialog.Bestaetigen = false;

        Assert.False(main.DarfFensterGeschlossenWerden());
        Assert.True(editor.HatUngespeicherteAenderungen);

        dialog.Bestaetigen = true;

        Assert.True(main.DarfFensterGeschlossenWerden());
        Assert.False(editor.HatUngespeicherteAenderungen);
        Assert.Equal(2, dialog.Bestaetigungen.Count);
    }

    private static (RechnungsEditorViewModel ViewModel, StubRechnungRepository Repository)
        ErstelleEditor(
            StubDialogService? dialogService = null,
            StubRechnungRepository? repository = null)
    {
        repository ??= new StubRechnungRepository();
        dialogService ??= new StubDialogService();
        var viewModel = new RechnungsEditorViewModel(
            new StubRepository<Kunde>([TestData.Kunde()]),
            new StubRepository<FirmaProfil>([TestData.FirmaProfil()]),
            new RechnungService(repository),
            dialogService);
        return (viewModel, repository);
    }

    private static (
        MainWindowViewModel Main,
        RechnungsEditorViewModel Editor,
        RechnungsUebersichtViewModel Uebersicht,
        KundenViewModel Kunden,
        StubRechnungRepository Repository,
        StubDialogService Dialog) ErstelleMainWindow()
    {
        var kundeRepository = new StubRepository<Kunde>([TestData.Kunde()]);
        var profilRepository = new StubRepository<FirmaProfil>([TestData.FirmaProfil()]);
        var rechnungRepository = new StubRechnungRepository();
        var service = new RechnungService(rechnungRepository);
        var dialog = new StubDialogService();
        var uebersicht = new RechnungsUebersichtViewModel(service, dialog);
        var editor = new RechnungsEditorViewModel(
            kundeRepository,
            profilRepository,
            service,
            dialog);
        var kunden = new KundenViewModel(kundeRepository, dialog);
        var main = new MainWindowViewModel(
            uebersicht,
            editor,
            kunden,
            new FirmenprofileViewModel(profilRepository, dialog),
            dialog);

        return (main, editor, uebersicht, kunden, rechnungRepository, dialog);
    }
}
