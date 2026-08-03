using System.Collections.ObjectModel;
using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public sealed class FirmenprofileViewModel : ViewModelBase
{
    private readonly IRepository<FirmaProfil> _firmaProfilRepository;
    private readonly IUserDialogService _dialogService;
    private readonly RelayCommand _bearbeitenCommand;
    private readonly AsyncRelayCommand _loeschenCommand;
    private readonly AsyncRelayCommand _speichernCommand;
    private readonly RelayCommand _abbrechenCommand;

    private FirmaProfil? _ausgewaehltesFirmaProfil;
    private FirmaProfil _firmaProfilEntwurf = new();
    private string _statusmeldung = string.Empty;
    private string _fehlermeldung = string.Empty;
    private bool _istEditorSichtbar;
    private bool _istBeschaeftigt;

    public FirmenprofileViewModel(
        IRepository<FirmaProfil> firmaProfilRepository,
        IUserDialogService dialogService)
    {
        _firmaProfilRepository = firmaProfilRepository
            ?? throw new ArgumentNullException(nameof(firmaProfilRepository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        NeuCommand = new RelayCommand(Neu, () => !IstBeschaeftigt);
        _bearbeitenCommand = new RelayCommand(
            Bearbeiten,
            () => AusgewaehltesFirmaProfil is not null && !IstBeschaeftigt);
        _loeschenCommand = new AsyncRelayCommand(
            LoeschenAsync,
            () => AusgewaehltesFirmaProfil?.Id is not null && !IstBeschaeftigt);
        _speichernCommand = new AsyncRelayCommand(
            SpeichernAsync,
            () => IstEditorSichtbar && !IstBeschaeftigt);
        _abbrechenCommand = new RelayCommand(
            Abbrechen,
            () => IstEditorSichtbar && !IstBeschaeftigt);
    }

    public ObservableCollection<FirmaProfil> Firmenprofile { get; } = [];

    public ICommand NeuCommand { get; }
    public ICommand BearbeitenCommand => _bearbeitenCommand;
    public ICommand LoeschenCommand => _loeschenCommand;
    public ICommand SpeichernCommand => _speichernCommand;
    public ICommand AbbrechenCommand => _abbrechenCommand;

    public FirmaProfil? AusgewaehltesFirmaProfil
    {
        get => _ausgewaehltesFirmaProfil;
        set
        {
            if (SetProperty(ref _ausgewaehltesFirmaProfil, value))
            {
                AktualisiereCommandStatus();
            }
        }
    }

    public FirmaProfil FirmaProfilEntwurf
    {
        get => _firmaProfilEntwurf;
        private set => SetProperty(ref _firmaProfilEntwurf, value);
    }

    public string Statusmeldung
    {
        get => _statusmeldung;
        private set => SetProperty(ref _statusmeldung, value);
    }

    public string Fehlermeldung
    {
        get => _fehlermeldung;
        private set => SetProperty(ref _fehlermeldung, value);
    }

    public bool IstEditorSichtbar
    {
        get => _istEditorSichtbar;
        private set
        {
            if (SetProperty(ref _istEditorSichtbar, value))
            {
                AktualisiereCommandStatus();
            }
        }
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

    public Task InitialisierenAsync() => LadeFirmenprofileAsync();

    private async Task LadeFirmenprofileAsync(int? auszuwaehlendeId = null)
    {
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            var profile = await _firmaProfilRepository.GetAllAsync();
            Firmenprofile.Clear();
            foreach (var profil in profile)
            {
                Firmenprofile.Add(profil);
            }

            AusgewaehltesFirmaProfil = auszuwaehlendeId is null
                ? null
                : Firmenprofile.FirstOrDefault(profil => profil.Id == auszuwaehlendeId);
            Statusmeldung = Firmenprofile.Count == 1
                ? "1 Firmenprofil vorhanden"
                : $"{Firmenprofile.Count} Firmenprofile vorhanden";
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Firmenprofile konnten nicht geladen werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Firmenprofile laden");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private void Neu()
    {
        FirmaProfilEntwurf = new FirmaProfil();
        Fehlermeldung = string.Empty;
        IstEditorSichtbar = true;
    }

    private void Bearbeiten()
    {
        if (AusgewaehltesFirmaProfil is null)
        {
            return;
        }

        FirmaProfilEntwurf = Kopiere(AusgewaehltesFirmaProfil);
        Fehlermeldung = string.Empty;
        IstEditorSichtbar = true;
    }

    private async Task SpeichernAsync()
    {
        Normalisiere(FirmaProfilEntwurf);
        var errors = FirmaProfilValidator.Validate(FirmaProfilEntwurf);
        if (errors.Count > 0)
        {
            Fehlermeldung = string.Join(Environment.NewLine, errors);
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            if (FirmaProfilEntwurf.Id is null)
            {
                await _firmaProfilRepository.CreateAsync(FirmaProfilEntwurf);
                Statusmeldung = $"{FirmaProfilEntwurf.Name} wurde angelegt.";
            }
            else
            {
                await _firmaProfilRepository.UpdateAsync(FirmaProfilEntwurf);
                Statusmeldung = $"{FirmaProfilEntwurf.Name} wurde aktualisiert.";
            }

            var id = FirmaProfilEntwurf.Id;
            IstEditorSichtbar = false;
            await LadeFirmenprofileAsync(id);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Das Firmenprofil konnte nicht gespeichert werden.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Firmenprofil speichern");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private async Task LoeschenAsync()
    {
        var profil = AusgewaehltesFirmaProfil;
        if (profil?.Id is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Soll das Firmenprofil „{profil.Name}“ wirklich gelöscht werden?",
                "Firmenprofil löschen"))
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _firmaProfilRepository.DeleteAsync(profil.Id.Value);
            Statusmeldung = $"{profil.Name} wurde gelöscht.";
            await LadeFirmenprofileAsync();
        }
        catch (Exception ex)
        {
            Fehlermeldung =
                "Das Firmenprofil kann nicht gelöscht werden, solange es von einer Rechnung verwendet wird.";
            _dialogService.ShowError(
                $"{Fehlermeldung}\n\n{ex.Message}",
                "Firmenprofil löschen");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private void Abbrechen()
    {
        IstEditorSichtbar = false;
        Fehlermeldung = string.Empty;
    }

    private void AktualisiereCommandStatus()
    {
        (NeuCommand as RelayCommand)?.RaiseCanExecuteChanged();
        _bearbeitenCommand.RaiseCanExecuteChanged();
        _loeschenCommand.RaiseCanExecuteChanged();
        _speichernCommand.RaiseCanExecuteChanged();
        _abbrechenCommand.RaiseCanExecuteChanged();
    }

    private static void Normalisiere(FirmaProfil profil)
    {
        profil.Name = profil.Name.Trim();
        profil.Ansprechpartner = profil.Ansprechpartner.Trim();
        profil.Strasse = profil.Strasse.Trim();
        profil.PLZ = profil.PLZ.Trim();
        profil.Ort = profil.Ort.Trim();
        profil.Land = profil.Land.Trim().ToUpperInvariant();
        profil.Email = profil.Email.Trim();
        profil.Telefon = profil.Telefon.Trim();
        profil.IBAN = profil.IBAN.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        profil.BIC = profil.BIC.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        profil.UstIdNr = string.IsNullOrWhiteSpace(profil.UstIdNr)
            ? null
            : profil.UstIdNr.Trim();
        profil.LogoPfad = profil.LogoPfad.Trim();
    }

    private static FirmaProfil Kopiere(FirmaProfil quelle) => new()
    {
        Id = quelle.Id,
        Name = quelle.Name,
        LogoPfad = quelle.LogoPfad,
        Ansprechpartner = quelle.Ansprechpartner,
        Strasse = quelle.Strasse,
        PLZ = quelle.PLZ,
        Ort = quelle.Ort,
        Land = quelle.Land,
        Email = quelle.Email,
        Telefon = quelle.Telefon,
        IBAN = quelle.IBAN,
        BIC = quelle.BIC,
        UstIdNr = quelle.UstIdNr
    };
}
