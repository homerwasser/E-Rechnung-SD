using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ERechnung.App.Services;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public sealed class KundenViewModel : INotifyPropertyChanged
{
    private readonly IRepository<Kunde> _kundeRepository;
    private readonly IUserDialogService _dialogService;
    private readonly List<Kunde> _alleKunden = [];
    private readonly RelayCommand _bearbeitenCommand;
    private readonly AsyncRelayCommand _loeschenCommand;
    private readonly AsyncRelayCommand _speichernCommand;

    private Kunde? _ausgewaehlterKunde;
    private Kunde _kundenEntwurf = new();
    private string _suchbegriff = string.Empty;
    private string _statusmeldung = string.Empty;
    private string _fehlermeldung = string.Empty;
    private bool _istEditorSichtbar;
    private bool _istBeschaeftigt;

    public KundenViewModel(IRepository<Kunde> kundeRepository, IUserDialogService dialogService)
    {
        _kundeRepository = kundeRepository ?? throw new ArgumentNullException(nameof(kundeRepository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        NeuCommand = new RelayCommand(Neu);
        SuchenCommand = new RelayCommand(WendeFilterAn);
        _bearbeitenCommand = new RelayCommand(Bearbeiten, () => AusgewaehlterKunde is not null && !IstBeschaeftigt);
        _loeschenCommand = new AsyncRelayCommand(LoeschenAsync, () => AusgewaehlterKunde?.Id is not null && !IstBeschaeftigt);
        _speichernCommand = new AsyncRelayCommand(SpeichernAsync, () => IstEditorSichtbar && !IstBeschaeftigt);
        AbbrechenCommand = new RelayCommand(Abbrechen, () => IstEditorSichtbar && !IstBeschaeftigt);
    }

    public ObservableCollection<Kunde> KundenListe { get; } = [];

    public ICommand NeuCommand { get; }
    public ICommand SuchenCommand { get; }
    public ICommand BearbeitenCommand => _bearbeitenCommand;
    public ICommand LoeschenCommand => _loeschenCommand;
    public ICommand SpeichernCommand => _speichernCommand;
    public ICommand AbbrechenCommand { get; }

    public Kunde? AusgewaehlterKunde
    {
        get => _ausgewaehlterKunde;
        set
        {
            if (SetField(ref _ausgewaehlterKunde, value))
            {
                AktualisiereCommandStatus();
            }
        }
    }

    public Kunde KundenEntwurf
    {
        get => _kundenEntwurf;
        private set => SetField(ref _kundenEntwurf, value);
    }

    public string Suchbegriff
    {
        get => _suchbegriff;
        set
        {
            if (SetField(ref _suchbegriff, value))
            {
                WendeFilterAn();
            }
        }
    }

    public string Statusmeldung
    {
        get => _statusmeldung;
        private set => SetField(ref _statusmeldung, value);
    }

    public string Fehlermeldung
    {
        get => _fehlermeldung;
        private set => SetField(ref _fehlermeldung, value);
    }

    public bool IstEditorSichtbar
    {
        get => _istEditorSichtbar;
        private set
        {
            if (SetField(ref _istEditorSichtbar, value))
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
            if (SetField(ref _istBeschaeftigt, value))
            {
                AktualisiereCommandStatus();
            }
        }
    }

    public async Task InitialisierenAsync() => await LadeKundenAsync();

    private async Task LadeKundenAsync(int? auszuwaehlendeId = null)
    {
        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            var kunden = await _kundeRepository.GetAllAsync();
            _alleKunden.Clear();
            _alleKunden.AddRange(kunden);
            WendeFilterAn();

            if (auszuwaehlendeId is not null)
            {
                AusgewaehlterKunde = KundenListe.FirstOrDefault(k => k.Id == auszuwaehlendeId);
            }
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Die Kunden konnten nicht geladen werden.";
            _dialogService.ShowError($"{Fehlermeldung}\n\n{ex.Message}", "Kunden laden");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private void WendeFilterAn()
    {
        var filter = Suchbegriff.Trim();
        var gefilterteKunden = string.IsNullOrWhiteSpace(filter)
            ? _alleKunden
            : _alleKunden.Where(k => EnthaeltSuchbegriff(k, filter)).ToList();

        KundenListe.Clear();
        foreach (var kunde in gefilterteKunden)
        {
            KundenListe.Add(kunde);
        }

        Statusmeldung = KundenListe.Count == 1
            ? "1 Kunde angezeigt"
            : $"{KundenListe.Count} Kunden angezeigt";
    }

    private static bool EnthaeltSuchbegriff(Kunde kunde, string suchbegriff)
    {
        return Enthält(kunde.Firmenname, suchbegriff)
            || Enthält(kunde.Ansprechpartner, suchbegriff)
            || Enthält(kunde.Ort, suchbegriff)
            || Enthält(kunde.Email, suchbegriff)
            || Enthält(kunde.UstIdNr, suchbegriff);
    }

    private static bool Enthält(string? wert, string suchbegriff) =>
        wert?.Contains(suchbegriff, StringComparison.CurrentCultureIgnoreCase) == true;

    private void Neu()
    {
        KundenEntwurf = new Kunde();
        Fehlermeldung = string.Empty;
        IstEditorSichtbar = true;
    }

    private void Bearbeiten()
    {
        if (AusgewaehlterKunde is null)
        {
            return;
        }

        KundenEntwurf = Kopiere(AusgewaehlterKunde);
        Fehlermeldung = string.Empty;
        IstEditorSichtbar = true;
    }

    private async Task SpeichernAsync()
    {
        Normalisiere(KundenEntwurf);
        var validierungsfehler = KundeValidator.Validate(KundenEntwurf);
        if (validierungsfehler.Count > 0)
        {
            Fehlermeldung = string.Join(Environment.NewLine, validierungsfehler);
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            if (KundenEntwurf.Id is null)
            {
                await _kundeRepository.CreateAsync(KundenEntwurf);
                Statusmeldung = $"{KundenEntwurf.Firmenname} wurde angelegt.";
            }
            else
            {
                await _kundeRepository.UpdateAsync(KundenEntwurf);
                Statusmeldung = $"{KundenEntwurf.Firmenname} wurde aktualisiert.";
            }

            var gespeicherteId = KundenEntwurf.Id;
            IstEditorSichtbar = false;
            await LadeKundenAsync(gespeicherteId);
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Der Kunde konnte nicht gespeichert werden.";
            _dialogService.ShowError($"{Fehlermeldung}\n\n{ex.Message}", "Kunde speichern");
        }
        finally
        {
            IstBeschaeftigt = false;
        }
    }

    private async Task LoeschenAsync()
    {
        var kunde = AusgewaehlterKunde;
        if (kunde?.Id is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Soll der Kunde „{kunde.Firmenname}“ wirklich gelöscht werden?",
                "Kunde löschen"))
        {
            return;
        }

        IstBeschaeftigt = true;
        Fehlermeldung = string.Empty;
        try
        {
            await _kundeRepository.DeleteAsync(kunde.Id.Value);
            Statusmeldung = $"{kunde.Firmenname} wurde gelöscht.";
            AusgewaehlterKunde = null;
            await LadeKundenAsync();
        }
        catch (Exception ex)
        {
            Fehlermeldung = "Der Kunde konnte nicht gelöscht werden. Möglicherweise ist er bereits mit einer Rechnung verknüpft.";
            _dialogService.ShowError($"{Fehlermeldung}\n\n{ex.Message}", "Kunde löschen");
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
        _bearbeitenCommand.RaiseCanExecuteChanged();
        _loeschenCommand.RaiseCanExecuteChanged();
        _speichernCommand.RaiseCanExecuteChanged();
        (AbbrechenCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static void Normalisiere(Kunde kunde)
    {
        kunde.Firmenname = kunde.Firmenname.Trim();
        kunde.Ansprechpartner = kunde.Ansprechpartner.Trim();
        kunde.Strasse = kunde.Strasse.Trim();
        kunde.PLZ = kunde.PLZ.Trim();
        kunde.Ort = kunde.Ort.Trim();
        kunde.Land = kunde.Land.Trim().ToUpperInvariant();
        kunde.Email = kunde.Email.Trim();
        kunde.Telefon = kunde.Telefon.Trim();
        kunde.UstIdNr = string.IsNullOrWhiteSpace(kunde.UstIdNr) ? null : kunde.UstIdNr.Trim();
        kunde.Bemerkung = kunde.Bemerkung.Trim();
    }

    private static Kunde Kopiere(Kunde quelle) => new()
    {
        Id = quelle.Id,
        Firmenname = quelle.Firmenname,
        Ansprechpartner = quelle.Ansprechpartner,
        Strasse = quelle.Strasse,
        PLZ = quelle.PLZ,
        Ort = quelle.Ort,
        Land = quelle.Land,
        Email = quelle.Email,
        Telefon = quelle.Telefon,
        UstIdNr = quelle.UstIdNr,
        Bemerkung = quelle.Bemerkung,
        ErstelltAm = quelle.ErstelltAm
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
