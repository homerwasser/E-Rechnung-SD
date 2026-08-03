using System.Collections.ObjectModel;
using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsUebersichtViewModel : ViewModelBase
{
    private readonly RechnungService _rechnungService;
    private readonly IUserDialogService _dialogService;
    private readonly AsyncRelayCommand _bearbeitenCommand;
    private readonly AsyncRelayCommand _loeschenCommand;
    private readonly AsyncRelayCommand _statusAendernCommand;

    private RechnungsListenEintragViewModel? _ausgewaehlteRechnung;
    private RechnungsStatusOption _ausgewaehlterFilter;
    private RechnungsStatusOption? _ausgewaehlterNeuerStatus;
    private string _fehlermeldung = string.Empty;
    private string _statusmeldung = string.Empty;
    private bool _istBeschaeftigt;

    public RechnungsUebersichtViewModel(
        RechnungService rechnungService,
        IUserDialogService dialogService)
    {
        _rechnungService = rechnungService ?? throw new ArgumentNullException(nameof(rechnungService));
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
    }

    public ObservableCollection<RechnungsListenEintragViewModel> Rechnungen { get; } = [];
    public IReadOnlyList<RechnungsStatusOption> FilterOptionen { get; }
    public IReadOnlyList<RechnungsStatusOption> StatusOptionen { get; }

    public ICommand NeuCommand { get; }
    public ICommand BearbeitenCommand => _bearbeitenCommand;
    public ICommand LoeschenCommand => _loeschenCommand;
    public ICommand FilterAnwendenCommand { get; }
    public ICommand StatusAendernCommand => _statusAendernCommand;

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
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            var rechnungen = await _rechnungService.GetAllAsync(AusgewaehlterFilter.Wert);
            Rechnungen.Clear();
            foreach (var rechnung in rechnungen)
            {
                Rechnungen.Add(new RechnungsListenEintragViewModel(rechnung));
            }

            AusgewaehlteRechnung = null;
            Statusmeldung = Rechnungen.Count == 1
                ? "1 Rechnung angezeigt"
                : $"{Rechnungen.Count} Rechnungen angezeigt";
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
        if (rechnung is null || status is null)
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _rechnungService.StatusAendernAsync(rechnung.Id, status);
            await LadeRechnungenAsync();
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
        if (rechnung is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Soll die Rechnung „{rechnung.Nummer}“ wirklich gelöscht werden?",
                "Rechnung löschen"))
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _rechnungService.DeleteAsync(rechnung.Id);
            await LadeRechnungenAsync();
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

    private void AktualisiereCommandStatus()
    {
        (NeuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _bearbeitenCommand.RaiseCanExecuteChanged();
        _loeschenCommand.RaiseCanExecuteChanged();
        (FilterAnwendenCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _statusAendernCommand.RaiseCanExecuteChanged();
    }
}
