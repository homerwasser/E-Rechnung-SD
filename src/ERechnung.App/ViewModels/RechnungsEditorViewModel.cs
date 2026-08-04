using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsEditorViewModel : ViewModelBase
{
    private readonly IRepository<Kunde> _kundeRepository;
    private readonly IRepository<FirmaProfil> _firmaProfilRepository;
    private readonly RechnungService _rechnungService;
    private readonly IUserDialogService _dialogService;
    private readonly RelayCommand<RechnungsPositionViewModel> _positionEntfernenCommand;
    private readonly AsyncRelayCommand _speichernCommand;
    private readonly AsyncRelayCommand _abbrechenCommand;

    private Rechnung _rechnungsEntwurf = new();
    private Kunde? _ausgewaehlterKunde;
    private FirmaProfil? _ausgewaehltesFirmaProfil;
    private string _titel = string.Empty;
    private DateTime? _rechnungsdatum = DateTime.Today;
    private DateTime? _leistungsdatum = DateTime.Today;
    private DateTime? _faelligkeitsdatum = DateTime.Today.AddDays(14);
    private string _status = RechnungsStatus.Erstellt;
    private string _bemerkung = string.Empty;
    private decimal _gesamtbetragNetto;
    private decimal _umsatzsteuerBetrag;
    private decimal _gesamtbetragBrutto;
    private string _fehlermeldung = string.Empty;
    private string _statusmeldung = string.Empty;
    private string _stammdatenHinweis = string.Empty;
    private string _snapshotHinweis = string.Empty;
    private bool _istBeschaeftigt;
    private bool _hatUngespeicherteAenderungen;
    private bool _aktualisierungAusgesetzt;
    private int _anzahlEingabefehler;

    public RechnungsEditorViewModel(
        IRepository<Kunde> kundeRepository,
        IRepository<FirmaProfil> firmaProfilRepository,
        RechnungService rechnungService,
        IUserDialogService dialogService)
    {
        _kundeRepository = kundeRepository ?? throw new ArgumentNullException(nameof(kundeRepository));
        _firmaProfilRepository = firmaProfilRepository
            ?? throw new ArgumentNullException(nameof(firmaProfilRepository));
        _rechnungService = rechnungService ?? throw new ArgumentNullException(nameof(rechnungService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        StatusOptionen = RechnungsStatus.Alle
            .Select(status => new RechnungsStatusOption(
                status,
                RechnungsStatus.GetAnzeigename(status)))
            .ToArray();

        PositionHinzufuegenCommand = new RelayCommand(PositionHinzufuegen, () => !IstBeschaeftigt);
        _positionEntfernenCommand = new RelayCommand<RechnungsPositionViewModel>(
            PositionEntfernen,
            position => position is not null && Positionen.Count > 1 && !IstBeschaeftigt);
        _speichernCommand = new AsyncRelayCommand(SpeichernAsync, () => !IstBeschaeftigt);
        _abbrechenCommand = new AsyncRelayCommand(AbbrechenAsync, () => !IstBeschaeftigt);
    }

    public ObservableCollection<Kunde> Kunden { get; } = [];
    public ObservableCollection<FirmaProfil> Firmenprofile { get; } = [];
    public ObservableCollection<RechnungsPositionViewModel> Positionen { get; } = [];
    public IReadOnlyList<RechnungsStatusOption> StatusOptionen { get; }

    public ICommand PositionHinzufuegenCommand { get; }
    public ICommand PositionEntfernenCommand => _positionEntfernenCommand;
    public ICommand SpeichernCommand => _speichernCommand;
    public ICommand AbbrechenCommand => _abbrechenCommand;

    public Func<Rechnung, Task>? RechnungGespeichertAsync { get; set; }
    public Func<Task>? BearbeitungAbgebrochenAsync { get; set; }

    public Rechnung RechnungsEntwurf
    {
        get => _rechnungsEntwurf;
        private set => SetProperty(ref _rechnungsEntwurf, value);
    }

    public string Ueberschrift => RechnungsEntwurf.Id is null
        ? "Neue Rechnung"
        : $"Rechnung {RechnungsEntwurf.Nummer} bearbeiten";

    public string Rechnungsnummer => string.IsNullOrWhiteSpace(RechnungsEntwurf.Nummer)
        ? "Wird beim Speichern vergeben"
        : RechnungsEntwurf.Nummer;

    public Kunde? AusgewaehlterKunde
    {
        get => _ausgewaehlterKunde;
        set => SetzeEingabewert(ref _ausgewaehlterKunde, value);
    }

    public FirmaProfil? AusgewaehltesFirmaProfil
    {
        get => _ausgewaehltesFirmaProfil;
        set => SetzeEingabewert(ref _ausgewaehltesFirmaProfil, value);
    }

    public string Titel
    {
        get => _titel;
        set => SetzeEingabewert(ref _titel, value ?? string.Empty);
    }

    public DateTime? Rechnungsdatum
    {
        get => _rechnungsdatum;
        set => SetzeEingabewert(ref _rechnungsdatum, value);
    }

    public DateTime? Leistungsdatum
    {
        get => _leistungsdatum;
        set => SetzeEingabewert(ref _leistungsdatum, value);
    }

    public DateTime? Faelligkeitsdatum
    {
        get => _faelligkeitsdatum;
        set => SetzeEingabewert(ref _faelligkeitsdatum, value);
    }

    public string Status
    {
        get => _status;
        set => SetzeEingabewert(ref _status, value ?? RechnungsStatus.Erstellt);
    }

    public string Bemerkung
    {
        get => _bemerkung;
        set => SetzeEingabewert(ref _bemerkung, value ?? string.Empty);
    }

    public decimal GesamtbetragNetto
    {
        get => _gesamtbetragNetto;
        private set => SetProperty(ref _gesamtbetragNetto, value);
    }

    public decimal UmsatzsteuerBetrag
    {
        get => _umsatzsteuerBetrag;
        private set => SetProperty(ref _umsatzsteuerBetrag, value);
    }

    public decimal GesamtbetragBrutto
    {
        get => _gesamtbetragBrutto;
        private set => SetProperty(ref _gesamtbetragBrutto, value);
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

    public string StammdatenHinweis
    {
        get => _stammdatenHinweis;
        private set
        {
            if (SetProperty(ref _stammdatenHinweis, value))
            {
                OnPropertyChanged(nameof(HatStammdatenHinweis));
            }
        }
    }

    public bool HatStammdatenHinweis => !string.IsNullOrWhiteSpace(StammdatenHinweis);

    public string SnapshotHinweis
    {
        get => _snapshotHinweis;
        private set
        {
            if (SetProperty(ref _snapshotHinweis, value))
            {
                OnPropertyChanged(nameof(HatSnapshotHinweis));
            }
        }
    }

    public bool HatSnapshotHinweis => !string.IsNullOrWhiteSpace(SnapshotHinweis);

    public bool HatUngespeicherteAenderungen
    {
        get => _hatUngespeicherteAenderungen;
        private set => SetProperty(ref _hatUngespeicherteAenderungen, value);
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

    public bool HatEingabefehler => _anzahlEingabefehler > 0;

    public async Task InitialisiereNeuAsync()
    {
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        Statusmeldung = string.Empty;
        try
        {
            await LadeStammdatenAsync();
            SetzeEntwurf(new Rechnung
            {
                Erstellungsdatum = DateTime.Today,
                Rechnungsdatum = DateTime.Today,
                Leistungsdatum = DateTime.Today,
                Faeligkeitsdatum = DateTime.Today.AddDays(14),
                Status = RechnungsStatus.Erstellt,
                Waehrung = "EUR",
                Positionen = [new RechnungsPosition()]
            });
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public async Task InitialisiereBearbeitungAsync(int rechnungId)
    {
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        Statusmeldung = string.Empty;
        try
        {
            var stammdatenTask = LadeStammdatenAsync();
            var rechnungTask = _rechnungService.GetByIdAsync(rechnungId);
            await Task.WhenAll(stammdatenTask, rechnungTask);

            var rechnung = await rechnungTask
                ?? throw new InvalidOperationException(
                    $"Die Rechnung mit der ID {rechnungId} wurde nicht gefunden.");
            SetzeEntwurf(rechnung);
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public void PositionHinzufuegen()
    {
        FuegePositionHinzu(new RechnungsPositionViewModel());
        AktualisiereSummen();
        MarkiereAlsGeaendert();
        AktualisiereCommandStatus();
    }

    public void PositionEntfernen(RechnungsPositionViewModel? position)
    {
        if (position is null || Positionen.Count <= 1 || !Positionen.Remove(position))
        {
            return;
        }

        position.WerteGeaendert -= Position_WerteGeaendert;
        AktualisiereSummen();
        MarkiereAlsGeaendert();
        AktualisiereCommandStatus();
    }

    public async Task SpeichernAsync()
    {
        Fehlermeldung = string.Empty;
        Statusmeldung = string.Empty;

        if (HatEingabefehler)
        {
            Fehlermeldung =
                "Bitte korrigieren Sie die markierten Zahlenfelder. Verwenden Sie das lokale Dezimaltrennzeichen.";
            return;
        }

        var stammdatenFehler = ErmittleStammdatenFehler();
        if (stammdatenFehler.Count > 0)
        {
            Fehlermeldung = string.Join(Environment.NewLine, stammdatenFehler);
            return;
        }

        AktualisiereEntwurfAusEingaben();
        IstBeschaeftigt = true;
        try
        {
            var gespeichert = await _rechnungService.SpeichernAsync(RechnungsEntwurf);
            RechnungsEntwurf = gespeichert;
            OnPropertyChanged(nameof(Ueberschrift));
            OnPropertyChanged(nameof(Rechnungsnummer));
            Statusmeldung = $"Rechnung {gespeichert.Nummer} wurde gespeichert.";
            HatUngespeicherteAenderungen = false;

            if (RechnungGespeichertAsync is not null)
            {
                await RechnungGespeichertAsync(gespeichert);
            }
        }
        catch (RechnungValidationException ex)
        {
            Fehlermeldung = string.Join(Environment.NewLine, ex.Errors);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Rechnung konnte nicht gespeichert werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Rechnung speichern");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    public void RegistriereEingabefehler(bool hinzugefuegt)
    {
        _anzahlEingabefehler = Math.Max(0, _anzahlEingabefehler + (hinzugefuegt ? 1 : -1));
        OnPropertyChanged(nameof(HatEingabefehler));
        if (hinzugefuegt)
        {
            MarkiereAlsGeaendert();
        }
    }

    public bool BestaetigeVerwerfen()
    {
        if (!HatUngespeicherteAenderungen)
        {
            return true;
        }

        if (!_dialogService.Confirm(
                "Die Rechnung enthält ungespeicherte Änderungen. Möchten Sie diese verwerfen?",
                "Ungespeicherte Änderungen"))
        {
            return false;
        }

        HatUngespeicherteAenderungen = false;
        return true;
    }

    public async Task AbbrechenAsync()
    {
        if (!BestaetigeVerwerfen())
        {
            return;
        }

        if (BearbeitungAbgebrochenAsync is not null)
        {
            await BearbeitungAbgebrochenAsync();
        }
    }

    private async Task LadeStammdatenAsync()
    {
        var kundenTask = _kundeRepository.GetAllAsync();
        var firmenprofileTask = _firmaProfilRepository.GetAllAsync();
        await Task.WhenAll(kundenTask, firmenprofileTask);

        Kunden.Clear();
        foreach (var kunde in await kundenTask)
        {
            Kunden.Add(kunde);
        }

        Firmenprofile.Clear();
        foreach (var firmaProfil in await firmenprofileTask)
        {
            Firmenprofile.Add(firmaProfil);
        }

        var hinweise = new List<string>();
        if (Kunden.Count == 0)
        {
            hinweise.Add("Es sind noch keine Kunden vorhanden. Legen Sie zuerst einen Kunden an.");
        }

        if (Firmenprofile.Count == 0)
        {
            hinweise.Add("Es sind noch keine Firmenprofile vorhanden. Legen Sie zuerst ein Firmenprofil an.");
        }

        StammdatenHinweis = string.Join(Environment.NewLine, hinweise);
    }

    private void SetzeEntwurf(Rechnung rechnung)
    {
        _aktualisierungAusgesetzt = true;
        try
        {
            RechnungsEntwurf = rechnung;
            SnapshotHinweis = ErstelleSnapshotHinweis(rechnung);
            Titel = rechnung.Titel;
            Rechnungsdatum = rechnung.Rechnungsdatum;
            Leistungsdatum = rechnung.Leistungsdatum;
            Faelligkeitsdatum = rechnung.Faeligkeitsdatum;
            Status = RechnungsStatus.IsValid(rechnung.Status)
                ? rechnung.Status
                : RechnungsStatus.Erstellt;
            Bemerkung = rechnung.Bemerkung;
            AusgewaehlterKunde = Kunden.FirstOrDefault(kunde => kunde.Id == rechnung.KundeId)
                ?? (rechnung.Id is null ? Kunden.FirstOrDefault() : null);
            AusgewaehltesFirmaProfil = Firmenprofile.FirstOrDefault(
                    firmaProfil => firmaProfil.Id == rechnung.FirmaProfilId)
                ?? (rechnung.Id is null ? Firmenprofile.FirstOrDefault() : null);

            EntferneAllePositionen();
            foreach (var position in rechnung.Positionen ?? [])
            {
                FuegePositionHinzu(new RechnungsPositionViewModel(position));
            }

            if (Positionen.Count == 0)
            {
                FuegePositionHinzu(new RechnungsPositionViewModel());
            }
        }
        finally
        {
            _aktualisierungAusgesetzt = false;
        }

        _anzahlEingabefehler = 0;
        OnPropertyChanged(nameof(HatEingabefehler));
        OnPropertyChanged(nameof(Ueberschrift));
        OnPropertyChanged(nameof(Rechnungsnummer));
        AktualisiereSummen();
        HatUngespeicherteAenderungen = false;
        AktualisiereCommandStatus();
    }

    private void FuegePositionHinzu(RechnungsPositionViewModel position)
    {
        position.WerteGeaendert += Position_WerteGeaendert;
        Positionen.Add(position);
    }

    private void EntferneAllePositionen()
    {
        foreach (var position in Positionen)
        {
            position.WerteGeaendert -= Position_WerteGeaendert;
        }

        Positionen.Clear();
    }

    private void Position_WerteGeaendert(object? sender, EventArgs e)
    {
        AktualisiereSummen();
        MarkiereAlsGeaendert();
    }

    private void AktualisiereSummen()
    {
        if (_aktualisierungAusgesetzt)
        {
            return;
        }

        RechnungsEntwurf.Positionen = Positionen
            .Select((position, index) => position.ToModel(index + 1))
            .ToList();
        RechnungCalculator.Berechnen(RechnungsEntwurf);
        GesamtbetragNetto = RechnungsEntwurf.GesamtbetragNetto;
        UmsatzsteuerBetrag = RechnungsEntwurf.UmsatzsteuerBetrag;
        GesamtbetragBrutto = RechnungsEntwurf.GesamtbetragBrutto;
    }

    private List<string> ErmittleStammdatenFehler()
    {
        var errors = new List<string>();
        if (Kunden.Count == 0)
        {
            errors.Add("Es ist kein Kunde vorhanden. Legen Sie zuerst einen Kunden an.");
        }
        else if (AusgewaehlterKunde?.Id is null or <= 0)
        {
            errors.Add("Bitte wählen Sie einen Kunden aus.");
        }

        if (Firmenprofile.Count == 0)
        {
            errors.Add("Es ist kein Firmenprofil vorhanden. Legen Sie zuerst ein Firmenprofil an.");
        }
        else if (AusgewaehltesFirmaProfil?.Id is null or <= 0)
        {
            errors.Add("Bitte wählen Sie ein Firmenprofil aus.");
        }

        return errors;
    }

    private void AktualisiereEntwurfAusEingaben()
    {
        RechnungsEntwurf.Titel = Titel.Trim();
        RechnungsEntwurf.Rechnungsdatum = Rechnungsdatum?.Date ?? default;
        RechnungsEntwurf.Leistungsdatum = Leistungsdatum?.Date;
        RechnungsEntwurf.Faeligkeitsdatum = Faelligkeitsdatum?.Date;
        RechnungsEntwurf.KundeId = AusgewaehlterKunde?.Id;
        RechnungsEntwurf.Kunde = AusgewaehlterKunde;
        RechnungsEntwurf.FirmaProfilId = AusgewaehltesFirmaProfil?.Id;
        RechnungsEntwurf.Absender = AusgewaehltesFirmaProfil;
        RechnungsEntwurf.Status = Status;
        RechnungsEntwurf.Waehrung = "EUR";
        RechnungsEntwurf.Bemerkung = Bemerkung.Trim();
        AktualisiereSummen();
    }

    private static string ErstelleSnapshotHinweis(Rechnung rechnung)
    {
        if (rechnung.Id is null)
        {
            return string.Empty;
        }

        var empfaengerName = string.IsNullOrWhiteSpace(rechnung.EmpfaengerSnapshot?.Name)
            ? "Name nicht verfügbar"
            : rechnung.EmpfaengerSnapshot.Name;
        var absenderName = string.IsNullOrWhiteSpace(rechnung.AbsenderSnapshot?.Name)
            ? "Name nicht verfügbar"
            : rechnung.AbsenderSnapshot.Name;

        return $"Historische Rechnungsdaten: Empfänger „{empfaengerName}“ und Absender „{absenderName}“ "
               + "stammen aus gespeicherten Snapshots. Änderungen an aktuellen Stammdaten verändern "
               + "diese Rechnung selbst bei gleicher ID nicht automatisch.";
    }

    private void SetzeEingabewert<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            MarkiereAlsGeaendert();
        }
    }

    private void MarkiereAlsGeaendert()
    {
        if (!_aktualisierungAusgesetzt)
        {
            HatUngespeicherteAenderungen = true;
        }
    }

    private void AktualisiereCommandStatus()
    {
        (PositionHinzufuegenCommand as RelayCommand)?.RaiseCanExecuteChanged();
        _positionEntfernenCommand.RaiseCanExecuteChanged();
        _speichernCommand.RaiseCanExecuteChanged();
        _abbrechenCommand.RaiseCanExecuteChanged();
    }
}
