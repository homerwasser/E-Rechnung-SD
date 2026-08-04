namespace ERechnung.Core.Models;

/// <summary>
/// Unabhängige Kopie der Absenderstammdaten zum Zeitpunkt der Rechnungserstellung.
/// </summary>
public class RechnungsAbsenderSnapshot
{
    private byte[]? _logoInhalt;

    public int QuellId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoPfad { get; set; } = string.Empty;
    public byte[]? LogoInhalt
    {
        get => _logoInhalt?.ToArray();
        set => _logoInhalt = value?.ToArray();
    }

    public string? LogoMedientyp { get; set; }
    public string Ansprechpartner { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Land { get; set; } = "DE";
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string? UstIdNr { get; set; }
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
}
