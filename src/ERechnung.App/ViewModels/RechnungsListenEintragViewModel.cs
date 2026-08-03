using ERechnung.Core.Models;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsListenEintragViewModel
{
    public RechnungsListenEintragViewModel(RechnungsUebersicht rechnung)
    {
        Model = rechnung ?? throw new ArgumentNullException(nameof(rechnung));
    }

    public RechnungsUebersicht Model { get; }
    public int Id => Model.Id;
    public string Nummer => Model.Nummer;
    public DateTime Rechnungsdatum => Model.Rechnungsdatum;
    public string KundeName => Model.KundeName;
    public decimal GesamtbetragBrutto => Model.GesamtbetragBrutto;
    public string Status => Model.Status;
    public string StatusAnzeige => RechnungsStatus.IsValid(Model.Status)
        ? RechnungsStatus.GetAnzeigename(Model.Status)
        : Model.Status;
}
