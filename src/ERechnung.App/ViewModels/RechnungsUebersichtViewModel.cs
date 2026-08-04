using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using ERechnung.XML.Generators;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsUebersichtViewModel : ViewModelBase
{
    private readonly RechnungService _rechnungService;
    private readonly RechnungsPdfService _rechnungsPdfService;
    private readonly IRechnungsPdfAblage _pdfAblage;
    private readonly IDateiOeffner _dateiOeffner;
    private readonly EmailEntwurfComposer _emailEntwurfComposer;
    private readonly IEmailEntwurfService _emailEntwurfService;
    private readonly IUserDialogService _dialogService;
    private readonly IUblGenerator _ublGenerator;
    private readonly AsyncRelayCommand _bearbeitenCommand;
    private readonly AsyncRelayCommand _loeschenCommand;
    private readonly AsyncRelayCommand _statusAendernCommand;
    private readonly AsyncRelayCommand _pdfErstellenAktualisierenCommand;
    private readonly RelayCommand _pdfOeffnenCommand;
    private readonly RelayCommand _imExplorerAnzeigenCommand;
    private readonly AsyncRelayCommand _emailEntwurfOeffnenCommand;
    private readonly AsyncRelayCommand _xmlExportierenCommand;
    private readonly RelayCommand _xmlImExplorerAnzeigenCommand;
    private string? _letzteXmlDatei;

    private RechnungsListenEintragViewModel? _ausgewaehlteRechnung;
    private RechnungsStatusOption _ausgewaehlterFilter;
    private RechnungsStatusOption? _ausgewaehlterNeuerStatus;
    private string _fehlermeldung = string.Empty;
    private string _statusmeldung = string.Empty;
    private bool _istBeschaeftigt;

    public RechnungsUebersichtViewModel(
        RechnungService rechnungService,
        RechnungsPdfService rechnungsPdfService,
        IRechnungsPdfAblage pdfAblage,
        IDateiOeffner dateiOeffner,
        EmailEntwurfComposer emailEntwurfComposer,
        IEmailEntwurfService emailEntwurfService,
        IUblGenerator ublGenerator,
        IUserDialogService dialogService)
    {
        _rechnungService = rechnungService ?? throw new ArgumentNullException(nameof(rechnungService));
        _rechnungsPdfService = rechnungsPdfService
            ?? throw new ArgumentNullException(nameof(rechnungsPdfService));
        _pdfAblage = pdfAblage ?? throw new ArgumentNullException(nameof(pdfAblage));
        _dateiOeffner = dateiOeffner ?? throw new ArgumentNullException(nameof(dateiOeffner));
        _emailEntwurfComposer = emailEntwurfComposer
            ?? throw new ArgumentNullException(nameof(emailEntwurfComposer));
        _emailEntwurfService = emailEntwurfService
            ?? throw new ArgumentNullException(nameof(emailEntwurfService));
        _ublGenerator = ublGenerator ?? throw new ArgumentNullException(nameof(ublGenerator));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        FilterOptionen = new[] { new RechnungsStatusOption(null, "Alle") }
            .Concat(RechnungsStatus.Alle.Select(status => new RechnungsStatusOption(
                status,
                RechnungsStatus.GetAnzeigename(status))))
            .ToArray();
        StatusOptionen = FilterOptionen.Skip(1).ToArray();
        _ausgewaehlterFilter = FilterOptionen[0];
        _ausgewaehlterNeuerStatus = StatusOptionen[0];

        NeuCommand = new AsyncRelayCommand(NeueRechnungAnfordernAsync, () => !IstBeschaeftigt);
        _bearbeitenCommand = new AsyncRelayCommand(
            BearbeitungAnfordernAsync,
            () => AusgewaehlteRechnung is not null && !IstBeschaeftigt);
        _loeschenCommand = new AsyncRelayCommand(
            LoeschenAsync,
            () => AusgewaehlteRechnung is not null && !IstBeschaeftigt);
        FilterAnwendenCommand = new AsyncRelayCommand(LadeRechnungenAsync, () => !IstBeschaeftigt);
        _statusAendernCommand = new AsyncRelayCommand(
            StatusAendernAsync,
            () => AusgewaehlteRechnung is not null
                  && AusgewaehlterNeuerStatus?.Wert is not null
                  && !IstBeschaeftigt);
        _pdfErstellenAktualisierenCommand = new AsyncRelayCommand(
            PdfErstellenAktualisierenAsync,
            () => AusgewaehlteRechnung?.KannPdfErstellen == true && !IstBeschaeftigt);
        _pdfOeffnenCommand = new RelayCommand(
            PdfOeffnen,
            () => AusgewaehlteRechnung?.KannPdfOeffnen == true && !IstBeschaeftigt);
        _imExplorerAnzeigenCommand = new RelayCommand(
            ImExplorerAnzeigen,
            () => AusgewaehlteRechnung?.KannImExplorerAnzeigen == true && !IstBeschaeftigt);
        _emailEntwurfOeffnenCommand = new AsyncRelayCommand(
            EmailEntwurfOeffnenAsync,
            () => AusgewaehlteRechnung?.KannEmailEntwurfOeffnen == true && !IstBeschaeftigt);
        _xmlExportierenCommand = new AsyncRelayCommand(
            XmlExportierenAsync,
            () => AusgewaehlteRechnung is not null && !IstBeschaeftigt);
        _xmlImExplorerAnzeigenCommand = new RelayCommand(
            XmlImExplorerAnzeigen,
            () => !string.IsNullOrWhiteSpace(_letzteXmlDatei) && !IstBeschaeftigt);
    }

    public ObservableCollection<RechnungsListenEintragViewModel> Rechnungen { get; } = [];
    public IReadOnlyList<RechnungsStatusOption> FilterOptionen { get; }
    public IReadOnlyList<RechnungsStatusOption> StatusOptionen { get; }

    public ICommand NeuCommand { get; }
    public ICommand BearbeitenCommand => _bearbeitenCommand;
    public ICommand LoeschenCommand => _loeschenCommand;
    public ICommand FilterAnwendenCommand { get; }
    public ICommand StatusAendernCommand => _statusAendernCommand;
    public ICommand PdfErstellenAktualisierenCommand => _pdfErstellenAktualisierenCommand;
    public ICommand PdfOeffnenCommand => _pdfOeffnenCommand;
    public ICommand ImExplorerAnzeigenCommand => _imExplorerAnzeigenCommand;
    public ICommand EmailEntwurfOeffnenCommand => _emailEntwurfOeffnenCommand;
    public ICommand XmlExportierenCommand => _xmlExportierenCommand;
    public ICommand XmlImExplorerAnzeigenCommand => _xmlImExplorerAnzeigenCommand;

    public Func<Task>? NeueRechnungAngefordertAsync { get; set; }
    public Func<int, Task>? BearbeitungAngefordertAsync { get; set; }

    public RechnungsListenEintragViewModel? AusgewaehlteRechnung
    {
        get => _ausgewaehlteRechnung;
        set
        {
            if (!SetProperty(ref _ausgewaehlteRechnung, value))
            {
                return;
            }

            if (value is not null)
            {
                AusgewaehlterNeuerStatus = StatusOptionen.FirstOrDefault(
                    option => option.Wert == value.Status);
            }

            AktualisiereCommandStatus();
        }
    }

    public RechnungsStatusOption AusgewaehlterFilter
    {
        get => _ausgewaehlterFilter;
        set => SetProperty(ref _ausgewaehlterFilter, value ?? FilterOptionen[0]);
    }

    public RechnungsStatusOption? AusgewaehlterNeuerStatus
    {
        get => _ausgewaehlterNeuerStatus;
        set
        {
            if (SetProperty(ref _ausgewaehlterNeuerStatus, value))
            {
                AktualisiereCommandStatus();
            }
        }
    }

    public string Fehlermeldung
    {
        get => _fehlermeldung;
        private set => SetProperty(ref _fehlermeldung, value);
    }

    public string Statusmeldung
    {
        get => _statusmeldung;
        private set => SetProperty(ref _statusmeldung, value);
    }

    public bool IstBeschaeftigt
    {
        get => _istBeschaeftigt;
        private set
        {
            if (SetProperty(ref _istBeschaeftigt, value))
            {
                AktualisiereCommandStatus();
            }
        }
    }

    public Task InitialisierenAsync() => LadeRechnungenAsync();

    public async Task LadeRechnungenAsync()
    {
        if (IstBeschaeftigt)
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await LadeRechnungenKernAsync(auswahlId: null);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Rechnungen konnten nicht geladen werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Rechnungen laden");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public async Task StatusAendernAsync()
    {
        var rechnung = AusgewaehlteRechnung;
        var status = AusgewaehlterNeuerStatus?.Wert;
        if (rechnung is null || status is null || IstBeschaeftigt)
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _rechnungService.StatusAendernAsync(rechnung.Id, status);
            await LadeRechnungenKernAsync(rechnung.Id);
            Statusmeldung = $"Der Status wurde auf „{RechnungsStatus.GetAnzeigename(status)}“ gesetzt.";
        }
        catch (RechnungValidationException ex)
        {
            Fehlermeldung = string.Join(Environment.NewLine, ex.Errors);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Der Rechnungsstatus konnte nicht geändert werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Status ändern");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public async Task PdfErstellenAktualisierenAsync()
    {
        var rechnung = AusgewaehlteRechnung;
        if (rechnung?.KannPdfErstellen != true || IstBeschaeftigt)
        {
            return;
        }

        var warBereitsVerknuepft = rechnung.HatPdfVerknuepfung;
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _rechnungsPdfService.ErzeugeAsync(rechnung.Id);
            await LadeRechnungenKernAsync(rechnung.Id);
            Statusmeldung = warBereitsVerknuepft
                ? $"Die PDF für Rechnung {rechnung.Nummer} wurde aktualisiert."
                : $"Die PDF für Rechnung {rechnung.Nummer} wurde erstellt.";
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Rechnungs-PDF konnte nicht erstellt werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "PDF erstellen");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public void PdfOeffnen()
    {
        var rechnung = AusgewaehlteRechnung;
        if (rechnung?.KannPdfOeffnen != true
            || rechnung.PdfVollstaendigerPfad is null
            || IstBeschaeftigt)
        {
            return;
        }

        Fehlermeldung = string.Empty;
        try
        {
            _dateiOeffner.Oeffne(rechnung.PdfVollstaendigerPfad);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die PDF konnte nicht geöffnet werden.";
            _dialogService.ShowError($"{Fehlermeldung}\n\n{ex.Message}", "PDF öffnen");
        }
    }

    public void ImExplorerAnzeigen()
    {
        var rechnung = AusgewaehlteRechnung;
        if (rechnung?.KannImExplorerAnzeigen != true
            || rechnung.PdfVollstaendigerPfad is null
            || IstBeschaeftigt)
        {
            return;
        }

        Fehlermeldung = string.Empty;
        try
        {
            _dateiOeffner.ImExplorerAnzeigen(rechnung.PdfVollstaendigerPfad);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die PDF konnte im Explorer nicht angezeigt werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Im Explorer anzeigen");
        }
    }

    public async Task EmailEntwurfOeffnenAsync()
    {
        var listenEintrag = AusgewaehlteRechnung;
        if (listenEintrag?.KannEmailEntwurfOeffnen != true
            || listenEintrag.PdfVollstaendigerPfad is null
            || IstBeschaeftigt)
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            var rechnung = await _rechnungService.GetByIdAsync(listenEintrag.Id)
                ?? throw new InvalidOperationException(
                    $"Die Rechnung mit der ID {listenEintrag.Id} wurde nicht gefunden.");
            var aktuellerPdfPfad = ErmittleAktuellenPdfPfad(rechnung);
            var entwurf = _emailEntwurfComposer.Erstelle(rechnung, aktuellerPdfPfad);
            var ergebnis = _emailEntwurfService.Oeffne(entwurf);

            switch (ergebnis.Status)
            {
                case EmailEntwurfErgebnisStatus.MitAnhangGeoeffnet:
                    Statusmeldung = "Der E-Mail-Entwurf wurde in klassischem Outlook mit PDF-Anhang geöffnet.";
                    break;
                case EmailEntwurfErgebnisStatus.OhneAnhangAngefordert:
                    Statusmeldung = "Der Standard-E-Mail-Client wurde ohne PDF-Anhang vorbereitet.";
                    _dialogService.ShowInfo(
                        "Der Standard-E-Mail-Client wurde ohne Anhang vorbereitet. "
                        + "Bitte hängen Sie die PDF manuell an. Die PDF wird jetzt im Explorer angezeigt.",
                        "E-Mail-Entwurf");
                    ZeigeFallbackPdfImExplorer(aktuellerPdfPfad);
                    break;
                case EmailEntwurfErgebnisStatus.Abgebrochen:
                    Statusmeldung = "Das Öffnen des E-Mail-Entwurfs wurde abgebrochen.";
                    break;
                default:
                    Fehlermeldung = ergebnis.Fehlermeldung
                        ?? "Der E-Mail-Entwurf konnte nicht geöffnet werden.";
                    _dialogService.ShowError(Fehlermeldung, "E-Mail-Entwurf öffnen");
                    break;
            }
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Der E-Mail-Entwurf konnte nicht geöffnet werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "E-Mail-Entwurf öffnen");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private string ErmittleAktuellenPdfPfad(Rechnung rechnung)
    {
        var verknuepfung = rechnung.PdfVerknuepfung
            ?? throw new InvalidOperationException(
                "Die Rechnung besitzt keine PDF-Verknüpfung mehr. Laden Sie die Übersicht neu.");

        if (verknuepfung.RechnungsstandAm != rechnung.GeaendertAm)
        {
            throw new InvalidOperationException(
                "Die Rechnung wurde seit der PDF-Erstellung geändert. Erstellen Sie die PDF erneut.");
        }

        if (!_pdfAblage.Existiert(verknuepfung.RelativerPfad))
        {
            throw new InvalidOperationException(
                "Die aktuelle Rechnungs-PDF wurde nicht gefunden. Erstellen Sie die PDF erneut.");
        }

        return _pdfAblage.LoeseVollstaendigenPfadAuf(verknuepfung.RelativerPfad);
    }

    public async Task XmlExportierenAsync()
    {
        var listenEintrag = AusgewaehlteRechnung;
        if (listenEintrag is null || IstBeschaeftigt)
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            var rechnung = await _rechnungService.GetByIdAsync(listenEintrag.Id)
                ?? throw new KeyNotFoundException(
                    $"Die Rechnung {listenEintrag.Nummer} wurde in der Datenbank nicht gefunden.");

            var xmlInhalt = _ublGenerator.Generate(rechnung);
            var xmlPfad = BerechneXmlPfad(rechnung);
            var xmlOrdner = Path.GetDirectoryName(xmlPfad)
                ?? throw new InvalidOperationException("Der XML-Zielordner konnte nicht bestimmt werden.");

            Directory.CreateDirectory(xmlOrdner);
            await File.WriteAllTextAsync(xmlPfad, xmlInhalt, Encoding.UTF8);

            _letzteXmlDatei = xmlPfad;
            _xmlImExplorerAnzeigenCommand.RaiseCanExecuteChanged();
            Statusmeldung = $"E-Rechnung-XML für {rechnung.Nummer} nach {xmlPfad} gespeichert.";
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Das E-Rechnung-XML konnte nicht exportiert werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "XML exportieren");
        }
        finally
        {
            IstBeschaeftigt = false;
            AktualisiereCommandStatus();
        }
    }

    public void XmlImExplorerAnzeigen()
    {
        if (string.IsNullOrWhiteSpace(_letzteXmlDatei) || !File.Exists(_letzteXmlDatei))
        {
            return;
        }

        try
        {
            _dateiOeffner.ImExplorerAnzeigen(_letzteXmlDatei!);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(
                $"Das XML-Verzeichnis konnte im Explorer nicht geöffnet werden.\n\n{ex.Message}",
                "Im Explorer anzeigen");
        }
    }

    private static string BerechneXmlPfad(Rechnung rechnung)
    {
        var lokalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var xmlBasisVerzeichnis = Path.Combine(lokalAppData, "ERechnung-SD", "xml");
        var jahresordner = rechnung.Rechnungsdatum.Year.ToString(
            "D4",
            CultureInfo.InvariantCulture);
        var bereinigterNummer = BereinigeDateiname(rechnung.Nummer);
        var timestamp = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        var dateiname = $"rechnung-{rechnung.Id!.Value}-{bereinigterNummer}-{timestamp}.xml";

        return Path.Combine(xmlBasisVerzeichnis, jahresordner, dateiname);
    }

    private static string BereinigeDateiname(string eingabe)
    {
        var ergebnis = new System.Text.StringBuilder();
        foreach (var zeichen in eingabe.Trim())
        {
            if (char.IsAsciiLetterOrDigit(zeichen))
            {
                ergebnis.Append(zeichen);
            }
            else if (ergebnis.Length > 0 && ergebnis[^1] != '-')
            {
                ergebnis.Append('-');
            }
        }
        var bereinigt = ergebnis.ToString().TrimEnd('-');
        return bereinigt.Length > 0 ? bereinigt : "ohne-nummer";
    }

    private async Task LadeRechnungenKernAsync(int? auswahlId)
    {
        var rechnungen = await _rechnungService.GetAllAsync(AusgewaehlterFilter.Wert);
        Rechnungen.Clear();
        foreach (var rechnung in rechnungen)
        {
            Rechnungen.Add(new RechnungsListenEintragViewModel(rechnung, _pdfAblage));
        }

        AusgewaehlteRechnung = auswahlId is null
            ? null
            : Rechnungen.FirstOrDefault(eintrag => eintrag.Id == auswahlId.Value);
        Statusmeldung = Rechnungen.Count == 1
            ? "1 Rechnung angezeigt"
            : $"{Rechnungen.Count} Rechnungen angezeigt";
    }

    private async Task NeueRechnungAnfordernAsync()
    {
        if (NeueRechnungAngefordertAsync is not null)
        {
            await NeueRechnungAngefordertAsync();
        }
    }

    private async Task BearbeitungAnfordernAsync()
    {
        if (AusgewaehlteRechnung is not null && BearbeitungAngefordertAsync is not null)
        {
            await BearbeitungAngefordertAsync(AusgewaehlteRechnung.Id);
        }
    }

    private async Task LoeschenAsync()
    {
        var rechnung = AusgewaehlteRechnung;
        if (rechnung is null || IstBeschaeftigt)
        {
            return;
        }

        string pdfHinweis = rechnung.HatPdfVerknuepfung
            ? "\n\nACHTUNG: Die zugehörige PDF-Datei wird ebenfalls unwiderruflich gelöscht."
            : string.Empty;

        if (!_dialogService.Confirm(
                $"Soll die Rechnung \"{rechnung.Nummer}\" wirklich gelöscht werden? Diese Aktion kann nicht rückgängig gemacht werden.{pdfHinweis}",
                "Rechnung löschen"))
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _rechnungService.DeleteAsync(rechnung.Id);
            await LadeRechnungenKernAsync(auswahlId: null);
            Statusmeldung = $"Rechnung {rechnung.Nummer} wurde gelöscht.";
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Rechnung konnte nicht gelöscht werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Rechnung löschen");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private void ZeigeFallbackPdfImExplorer(string pdfPfad)
    {
        try
        {
            _dateiOeffner.ImExplorerAnzeigen(pdfPfad);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(
                $"Die PDF konnte nicht im Explorer angezeigt werden.\n\n{ex.Message}",
                "PDF manuell anhängen");
        }
    }

    private void AktualisiereCommandStatus()
    {
        (NeuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _bearbeitenCommand.RaiseCanExecuteChanged();
        _loeschenCommand.RaiseCanExecuteChanged();
        (FilterAnwendenCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _statusAendernCommand.RaiseCanExecuteChanged();
        _pdfErstellenAktualisierenCommand.RaiseCanExecuteChanged();
        _pdfOeffnenCommand.RaiseCanExecuteChanged();
        _imExplorerAnzeigenCommand.RaiseCanExecuteChanged();
        _emailEntwurfOeffnenCommand.RaiseCanExecuteChanged();
        _xmlExportierenCommand.RaiseCanExecuteChanged();
        _xmlImExplorerAnzeigenCommand.RaiseCanExecuteChanged();
    }
}
