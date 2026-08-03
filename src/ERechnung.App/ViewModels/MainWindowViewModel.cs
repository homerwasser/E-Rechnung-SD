using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;

namespace ERechnung.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly RechnungsUebersichtViewModel _rechnungsUebersichtViewModel;
    private readonly RechnungsEditorViewModel _rechnungsEditorViewModel;
    private readonly KundenViewModel _kundenViewModel;
    private readonly FirmenprofileViewModel _firmenprofileViewModel;
    private readonly IUserDialogService _dialogService;
    private object _currentViewModel;
    private bool _istBeschaeftigt;

    public MainWindowViewModel(
        RechnungsUebersichtViewModel rechnungsUebersichtViewModel,
        RechnungsEditorViewModel rechnungsEditorViewModel,
        KundenViewModel kundenViewModel,
        FirmenprofileViewModel firmenprofileViewModel,
        IUserDialogService dialogService)
    {
        _rechnungsUebersichtViewModel = rechnungsUebersichtViewModel
            ?? throw new ArgumentNullException(nameof(rechnungsUebersichtViewModel));
        _rechnungsEditorViewModel = rechnungsEditorViewModel
            ?? throw new ArgumentNullException(nameof(rechnungsEditorViewModel));
        _kundenViewModel = kundenViewModel ?? throw new ArgumentNullException(nameof(kundenViewModel));
        _firmenprofileViewModel = firmenprofileViewModel
            ?? throw new ArgumentNullException(nameof(firmenprofileViewModel));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _currentViewModel = _rechnungsUebersichtViewModel;

        RechnungenCommand = new AsyncRelayCommand(RechnungenOeffnenAsync, () => !IstBeschaeftigt);
        NeueRechnungCommand = new AsyncRelayCommand(NeueRechnungOeffnenAsync, () => !IstBeschaeftigt);
        KundenCommand = new AsyncRelayCommand(KundenOeffnenAsync, () => !IstBeschaeftigt);
        FirmenprofileCommand = new AsyncRelayCommand(FirmenprofileOeffnenAsync, () => !IstBeschaeftigt);

        _rechnungsUebersichtViewModel.NeueRechnungAngefordertAsync = NeueRechnungOeffnenAsync;
        _rechnungsUebersichtViewModel.BearbeitungAngefordertAsync = RechnungBearbeitenAsync;
        _rechnungsEditorViewModel.RechnungGespeichertAsync = RechnungGespeichertAsync;
        _rechnungsEditorViewModel.BearbeitungAbgebrochenAsync = RechnungenOeffnenAsync;
    }

    public ICommand RechnungenCommand { get; }
    public ICommand DashboardCommand => RechnungenCommand;
    public ICommand NeueRechnungCommand { get; }
    public ICommand KundenCommand { get; }
    public ICommand FirmenprofileCommand { get; }

    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
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

    public Task InitialisierenAsync() => RechnungenOeffnenAsync();

    public bool DarfFensterGeschlossenWerden() =>
        !ReferenceEquals(CurrentViewModel, _rechnungsEditorViewModel)
        || _rechnungsEditorViewModel.BestaetigeVerwerfen();

    public Task RechnungenOeffnenAsync() => NavigiereAsync(
        _rechnungsUebersichtViewModel.InitialisierenAsync,
        _rechnungsUebersichtViewModel,
        "Rechnungsübersicht öffnen");

    public Task NeueRechnungOeffnenAsync() => NavigiereAsync(
        _rechnungsEditorViewModel.InitialisiereNeuAsync,
        _rechnungsEditorViewModel,
        "Neue Rechnung öffnen");

    public Task RechnungBearbeitenAsync(int rechnungId) => NavigiereAsync(
        () => _rechnungsEditorViewModel.InitialisiereBearbeitungAsync(rechnungId),
        _rechnungsEditorViewModel,
        "Rechnung öffnen");

    public Task KundenOeffnenAsync() => NavigiereAsync(
        _kundenViewModel.InitialisierenAsync,
        _kundenViewModel,
        "Kundenverwaltung öffnen");

    public Task FirmenprofileOeffnenAsync() => NavigiereAsync(
        _firmenprofileViewModel.InitialisierenAsync,
        _firmenprofileViewModel,
        "Firmenprofile öffnen");

    private async Task RechnungGespeichertAsync(Rechnung rechnung)
    {
        await RechnungenOeffnenAsync();
    }

    private async Task NavigiereAsync(
        Func<Task> laden,
        object zielViewModel,
        string fehlerTitel)
    {
        if (ReferenceEquals(CurrentViewModel, _rechnungsEditorViewModel)
            && !_rechnungsEditorViewModel.BestaetigeVerwerfen())
        {
            return;
        }

        IstBeschaeftigt = true;
        try
        {
            await laden();
            CurrentViewModel = zielViewModel;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(
                $"Die Ansicht konnte nicht geladen werden.\n\n{ex.Message}",
                fehlerTitel);
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private void AktualisiereCommandStatus()
    {
        (RechnungenCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NeueRechnungCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (KundenCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (FirmenprofileCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
