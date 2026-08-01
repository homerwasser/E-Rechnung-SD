using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Migrations;

public static class InitialMigration
{
    public static async Task AusfuehrenAsync(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        // Kunde Tabelle
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS tbl_Kunde (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Firmenname TEXT, Ansprechpartner TEXT, Strasse TEXT, PLZ TEXT, Ort TEXT, Land TEXT DEFAULT 'DE',
                Email TEXT, Telefon TEXT, UstIdNr TEXT, ErstelltAm DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ");

        // Rechnung Tabelle
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS tbl_Rechnung (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, Nummer TEXT, Titel TEXT, KundeId INTEGER, FirmaProfilId INTEGER,
                Erstellungsdatum DATE, Rechnungsdatum DATE, Faeligkeitsdatum DATE,
                GesamtNetto REAL, GesamtUst REAL, GesamtBrutto REAL, Status TEXT DEFAULT 'Erstellt',
                Bemerkung TEXT, ErstelltAm DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ");

        // RechnungsPosition Tabelle
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS tbl_RechnungsPosition (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, RechnungId INTEGER, Beschreibung TEXT,
                Menge REAL, Einheit TEXT, EinzelpreisNetto REAL, Steuersatz REAL
            )
        ");

        // FirmaProfil Tabelle
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS tbl_FirmaProfil (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, LogoPfad TEXT, Ansprechpartner TEXT,
                Strasse TEXT, PLZ TEXT, Ort TEXT, Email TEXT, Telefon TEXT, IBAN TEXT, BIC TEXT, UstIdNr TEXT
            )
        ");
    }
}