namespace ERechnung.Core.Models;

/// <summary>
/// Eine einzelne Position innerhalb einer Rechnung.
/// </summary>
public class RechnungsPosition
{
    public int? Id { get; set; }
    public int? RechnungId { get; set; }
    public int Reihenfolge { get; set; }

    public string Beschreibung { get; set; } = string.Empty;
    public decimal Menge { get; set; } = 1m;
    public string Einheit { get; set; } = "ST";

    public decimal EinzelpreisNetto { get; set; }
    public decimal Steuersatz { get; set; } = 19m;

    public decimal GesamtpreisNetto => decimal.Round(
        Menge * EinzelpreisNetto,
        2,
        MidpointRounding.AwayFromZero);
}