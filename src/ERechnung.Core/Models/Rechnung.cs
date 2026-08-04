namespace ERechnung.Core.Models;

/// <summary>
/// Eine Rechnung mit den für ihre weitere Verarbeitung benötigten Daten.
/// </summary>
public class Rechnung
{
    public int? Id { get; set; }
    public string Nummer { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public DateTime Erstellungsdatum { get; set; } = DateTime.Today;
    public DateTime Rechnungsdatum { get; set; } = DateTime.Today;
    public DateTime? Leistungsdatum { get; set; }
    public DateTime? Faeligkeitsdatum { get; set; }

    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }
    public RechnungsEmpfaengerSnapshot? EmpfaengerSnapshot { get; set; }

    public int? FirmaProfilId { get; set; }
    public FirmaProfil? Absender { get; set; }
    public RechnungsAbsenderSnapshot? AbsenderSnapshot { get; set; }

    public decimal GesamtbetragNetto { get; set; }
    public decimal UmsatzsteuerBetrag { get; set; }
    public decimal GesamtsteuerRate { get; set; }
    public decimal GesamtbetragBrutto { get; set; }
    public string Waehrung { get; set; } = "EUR";

    public List<RechnungsPosition> Positionen { get; set; } = [];

    public string Status { get; set; } = RechnungsStatus.Erstellt;
    public string Bemerkung { get; set; } = string.Empty;

    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public DateTime GeaendertAm { get; set; } = DateTime.UtcNow;
    public RechnungsPdfVerknuepfung? PdfVerknuepfung { get; set; }
}