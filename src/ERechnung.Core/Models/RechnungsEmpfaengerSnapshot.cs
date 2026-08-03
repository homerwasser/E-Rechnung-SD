namespace ERechnung.Core.Models;

/// <summary>
/// Unabhängige Kopie der Empfängerstammdaten zum Zeitpunkt der Rechnungserstellung.
/// </summary>
public class RechnungsEmpfaengerSnapshot
{
    public int QuellId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ansprechpartner { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Land { get; set; } = "DE";
    public string Email { get; set; } = string.Empty;
    public string? UstIdNr { get; set; }
}
