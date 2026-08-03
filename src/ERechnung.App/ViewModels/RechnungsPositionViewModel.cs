using System.Runtime.CompilerServices;
using ERechnung.Core.Models;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsPositionViewModel : ViewModelBase
{
    private readonly int? _id;
    private readonly int? _rechnungId;
    private string _beschreibung = string.Empty;
    private decimal _menge = 1m;
    private string _einheit = "ST";
    private decimal _einzelpreisNetto;
    private decimal _steuersatz = 19m;

    public RechnungsPositionViewModel()
    {
    }

    public RechnungsPositionViewModel(RechnungsPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        _id = position.Id;
        _rechnungId = position.RechnungId;
        _beschreibung = position.Beschreibung;
        _menge = position.Menge;
        _einheit = position.Einheit;
        _einzelpreisNetto = position.EinzelpreisNetto;
        _steuersatz = position.Steuersatz;
    }

    public event EventHandler? WerteGeaendert;

    public string Beschreibung
    {
        get => _beschreibung;
        set => SetWert(ref _beschreibung, value ?? string.Empty);
    }

    public decimal Menge
    {
        get => _menge;
        set
        {
            if (SetWert(ref _menge, value))
            {
                OnPropertyChanged(nameof(Positionsnetto));
            }
        }
    }

    public string Einheit
    {
        get => _einheit;
        set => SetWert(ref _einheit, value ?? string.Empty);
    }

    public decimal EinzelpreisNetto
    {
        get => _einzelpreisNetto;
        set
        {
            if (SetWert(ref _einzelpreisNetto, value))
            {
                OnPropertyChanged(nameof(Positionsnetto));
            }
        }
    }

    public decimal Steuersatz
    {
        get => _steuersatz;
        set => SetWert(ref _steuersatz, value);
    }

    public decimal Positionsnetto => decimal.Round(
        Menge * EinzelpreisNetto,
        2,
        MidpointRounding.AwayFromZero);

    public RechnungsPosition ToModel(int reihenfolge) => new()
    {
        Id = _id,
        RechnungId = _rechnungId,
        Reihenfolge = reihenfolge,
        Beschreibung = Beschreibung.Trim(),
        Menge = Menge,
        Einheit = Einheit.Trim(),
        EinzelpreisNetto = EinzelpreisNetto,
        Steuersatz = Steuersatz
    };

    private bool SetWert<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return false;
        }

        WerteGeaendert?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
