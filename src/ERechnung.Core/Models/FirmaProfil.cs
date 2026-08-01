using System;

namespace ERechnung.Core.Models;

/// <summary>
/// Firmenprofil (Marken: Tanzschule SDA, Entertainer & Choreograph)
/// </summary>
public class FirmaProfil
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;          // z.B. "Tanzschule SDA"
    public string LogoPfad { get; set; } = string.Empty;
    public string Ansprechpartner { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
    public string? UstIdNr { get; set; }
}