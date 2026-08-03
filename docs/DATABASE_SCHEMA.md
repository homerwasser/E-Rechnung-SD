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
| 2 | Firmenprofil-Land, Rechnungswährung, Stammdaten-Snapshots und jährliche Rechnungsnummernsequenz |

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

## M3: Firmenprofile

`tbl_FirmaProfil` enthält unterschiedliche Absender beziehungsweise Marken. Name, Anschrift, Land, Kontakt-, Bank-, Steuer- und lokaler Logo-Pfad werden pro Profil gespeichert. Repository und WPF-Oberfläche unterstützen vollständiges CRUD.

Ein Firmenprofil, das von einer Rechnung referenziert wird, kann wegen des Fremdschlüssels nicht gelöscht werden. Reale Firmenlogos und Stammdaten gehören ausschließlich in lokale, ignorierte Speicherorte.

## M3: Rechnungen und Positionen

`tbl_Rechnung` und `tbl_RechnungsPosition` bilden ein Master/Detail-Aggregat. Anlegen, Aktualisieren und Löschen erfolgen transaktional. Ein Fehler beim Speichern einer Position rollt auch Rechnungskopf und Nummernreservierung zurück. Beim Aktualisieren dient `GeaendertAm` als optimistischer Concurrency-Token, damit eine veraltete zweite Bearbeitung keine neueren Änderungen überschreibt.

Wichtige Rechnungsfelder:

- eindeutige `Nummer` im Format `JJJJ-NNN`
- Rechnungs- und optionales Fälligkeitsdatum
- Kunde und Firmenprofil als geschützte Fremdschlüssel
- Netto-, Steuer- und Bruttosumme
- Währung, aktuell standardmäßig `EUR`
- kanonischer Status
- Bemerkung und technische Zeitstempel
- strukturierte Empfänger- und Absender-Snapshots

Snapshots bewahren die bei der Rechnungserstellung verwendeten Namen, Anschriften, Kontakt-, Bank- und Steuerdaten. Spätere Änderungen an Kunden oder Firmenprofilen verändern damit keine bereits gespeicherte Rechnung.

Positionen enthalten Beschreibung, Menge, Einheit, Nettopreis, Steuersatz und eine stabile Reihenfolge. Beim Löschen einer Rechnung werden ihre Positionen per Cascade entfernt.

## Rechnungsnummern

`tbl_RechnungsnummerSequenz` führt die zuletzt vergebene laufende Nummer je Kalenderjahr. Reservierung und Rechnungsanlage laufen in derselben sofortigen SQLite-Transaktion. Bei einem Rollback wird keine Nummer verbraucht. Vorhandene Nummern im Format `JJJJ-<Ziffern>` werden beim Upgrade auf Migration 2 berücksichtigt.

## Backup

Backup und Wiederherstellung werden in M7 implementiert. Vor einer Wiederherstellung muss die Sicherung mit `PRAGMA integrity_check` geprüft werden.
