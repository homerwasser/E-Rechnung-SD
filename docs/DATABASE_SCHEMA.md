# Datenbank – E-Rechnung SD

## Speicherorte

| Inhalt | Pfad |
|---|---|
| Datenbank | `%LOCALAPPDATA%\ERechnung-SD\data\erechnung.db` |
| Backups (M7) | `%LOCALAPPDATA%\ERechnung-SD\backups\` |

Die Datenbank liegt bewusst außerhalb des Programm- und Repositoryordners. Eine Neuinstallation oder ein Programmupdate darf die Nutzerdaten nicht ersetzen.

## Verbindung

Die Anwendung nutzt `Microsoft.Data.Sqlite` und Dapper mit:

- Connection Pooling
- Foreign Keys
- gemeinsamem SQLite-Cache
- expliziten Transaktionen für schreibende Repository-Operationen

## Migrationen

`DatabaseMigrator` legt zuerst die Tabelle `SchemaMigrations` an. Jede Migration besitzt eine eindeutige Versionsnummer und wird innerhalb einer Transaktion genau einmal ausgeführt.

```sql
CREATE TABLE SchemaMigrations (
    Version   INTEGER PRIMARY KEY,
    Name      TEXT NOT NULL,
    AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

Aktueller Stand:

| Version | Inhalt |
|---|---|
| 1 | Kunden, Firmenprofile, Rechnungen, Positionen und Indizes |

## M2: Kunden

Wichtige Felder in `tbl_Kunde`:

- `Id` – technischer Primärschlüssel
- `Firmenname` – Pflichtfeld
- `Ansprechpartner`
- `Strasse`, `PLZ`, `Ort`, `Land`
- `Email`, `Telefon`
- `UstIdNr`
- `Bemerkung`
- `ErstelltAm`

Indizes unterstützen die Suche nach Firmenname und E-Mail.

## Firmenprofile

`tbl_FirmaProfil` enthält die unterschiedlichen Absender bzw. Marken. Logo, Anschrift, Bankdaten und Steuerdaten werden pro Profil gespeichert. Repository und Einstellungsoberfläche folgen in einem späteren Meilenstein.

## Rechnungen und Positionen

Die Tabellen sind im initialen Schema vorbereitet. Die Geschäftslogik und das transaktionale Master/Detail-Repository werden in M3 umgesetzt.

Fremdschlüssel schützen referenzierte Kunden und Firmenprofile vor versehentlichem Löschen. Positionen werden beim Löschen einer Rechnung kaskadierend entfernt.

## Backup

Backup und Wiederherstellung werden in M7 implementiert. Vor einer Wiederherstellung muss die Sicherung mit `PRAGMA integrity_check` geprüft werden.
