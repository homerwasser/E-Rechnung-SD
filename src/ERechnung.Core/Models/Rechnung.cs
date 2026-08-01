using System;
using System.Collections.Generic;

namespace ERechnung.Core.Models;

/// <summary>
/// Hauptklasse fuer eine Rechnung
/// </summary>
public class Rechnung
{
    public int? Id { get; set; }
    public string Nummer { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;           // z.B. Anlass / Veranstaltung
    public DateTime Erstellungsdatum { get; set; } = DateTime.Today;
    public DateTime Rechnungsdatum { get; set; } = DateTime.Today;
    public DateTime? Faeligkeitsdatum { get; set; }
    
    // Fremdschlüssel
    public int? KundeId { get; set; }
    public Kunde? Kunde { get; set; }
    
    // Fremdschlüssel
    public int? FirmaProfilId { get; set; }
    public FirmaProfil? Absender { get; set; }
    
    // Finanzen
    public decimal GesamtbetragNetto { get; set; }
    public decimal UmsatzsteuerBetrag { get; set; }
    public decimal GesamtsteuerRate { get; set; }
    public decimal GesamtbetragBrutto { get; set; }
    
    // Positionen
    public List<RechnungsPosition> Positionen { get; set; } = new();
    
    // Status
    public string Status { get; set; } = "Erstellt"; // Erstellt, Versendet, Offen,Bezahlt, InKlarung,Storniert
    
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    public DateTime GeandertAm { get; set; } = DateTime.Now;
}