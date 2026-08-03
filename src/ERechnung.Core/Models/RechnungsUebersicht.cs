namespace ERechnung.Core.Models;

/// <summary>
/// Schlanke Projektion für Rechnungslisten.
/// </summary>
public class RechnungsUebersicht
{
    public int Id { get; set; }
    public string Nummer { get; set; } = string.Empty;
    public DateTime Rechnungsdatum { get; set; }
    public string KundeName { get; set; } = string.Empty;
    public decimal GesamtbetragBrutto { get; set; }
    public string Status { get; set; } = RechnungsStatus.Erstellt;
}
