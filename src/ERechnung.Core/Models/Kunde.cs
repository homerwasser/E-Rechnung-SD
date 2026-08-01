using System;

namespace ERechnung.Core.Models;

/// <summary>
/// Kunde / Empfänger der Rechnung
/// </summary>
public class Kunde
{
    public int? Id { get; set; }
    public string Firmenname { get; set; } = string.Empty;
    public string Ansprechpartner { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Land { get; set; } = "DE";
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string? UstIdNr { get; set; }
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
}