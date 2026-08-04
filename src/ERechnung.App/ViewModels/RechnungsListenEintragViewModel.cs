using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public sealed class RechnungsListenEintragViewModel
{
    public RechnungsListenEintragViewModel(
        RechnungsUebersicht rechnung,
        IRechnungsPdfAblage pdfAblage)
    {
        Model = rechnung ?? throw new ArgumentNullException(nameof(rechnung));
        ArgumentNullException.ThrowIfNull(pdfAblage);

        HatPdfVerknuepfung = !string.IsNullOrWhiteSpace(Model.PdfRelativerPfad);
        if (!HatPdfVerknuepfung)
        {
            PdfStatus = "Kein PDF";
            return;
        }

        PdfDateiExistiert = ErmittlePdfDatei(pdfAblage, out var vollstaendigerPfad);
        PdfVollstaendigerPfad = vollstaendigerPfad;

        if (Model.PdfRechnungsstandAm != Model.GeaendertAm)
        {
            PdfStatus = "Veraltet";
            return;
        }

        PdfStatus = PdfDateiExistiert ? "Aktuell" : "Datei fehlt";
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

    public string PdfStatus { get; }
    public bool HatPdfVerknuepfung { get; }
    public bool PdfDateiExistiert { get; }
    public string? PdfVollstaendigerPfad { get; }
    public bool IstPdfAktuell => PdfStatus == "Aktuell";
    public bool KannPdfErstellen => Id > 0;
    public bool KannPdfOeffnen => PdfDateiExistiert && PdfVollstaendigerPfad is not null;
    public bool KannImExplorerAnzeigen => KannPdfOeffnen;
    public bool KannEmailEntwurfOeffnen => IstPdfAktuell && KannPdfOeffnen;

    private bool ErmittlePdfDatei(
        IRechnungsPdfAblage pdfAblage,
        out string? vollstaendigerPfad)
    {
        vollstaendigerPfad = null;
        try
        {
            if (!pdfAblage.Existiert(Model.PdfRelativerPfad!))
            {
                return false;
            }

            vollstaendigerPfad = pdfAblage.LoeseVollstaendigenPfadAuf(Model.PdfRelativerPfad!);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
