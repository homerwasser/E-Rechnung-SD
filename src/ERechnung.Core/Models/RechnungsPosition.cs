namespace ERechnung.Core.Models;

/// <summary>
/// Eine einzelne Position innerhalb einer Rechnung.
/// </summary>
public class RechnungsPosition
{
    public int? Id { get; set; }
    public int? RechnungId { get; set; }
    
    public string Beschreibung { get; set; } = string.Empty;
    public decimal Menge { get; set; } = 1m;
    public string Einheit { get; set; } = "ST"; // ST, Tage, Std, km
    
    public decimal EinzelpreisNetto { get; set; }
    public decimal Steuersatz { get; set; } = 19m; // Prozent (0, 7, 19, 25)
    public decimal GesamtpreisNetto => Menge * EinzelpreisNetto;
}