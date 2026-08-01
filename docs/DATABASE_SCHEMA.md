# Datenbank-Schema – E-Rechnung SD

## Speicherort

| Umgebungsvariable | Pfad                                    | Inhalt                     |
|-------------------|-----------------------------------------|----------------------------|
| `DATA_DIR`        | `%LOCALAPPDATA%\ERechnung-SD\data\`     | Datenbankdatei             |
| `BACKUP_DIR`      | `%LOCALAPPDATA%\ERechnung-SD\backups\`  | Backup-Dateien             |

## Tabellen

### tbl_Rechnung (Rechnungen)

```sql
CREATE TABLE tbl_Rechnung (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Nummer          TEXT    NOT NULL UNIQUE,  -- z.B. "2024-001"
    Titel           TEXT,                     -- z.B. "Anlass / Veranstaltung"
    Erstellungsdatum DATE NOT NULL DEFAULT CURRENT_DATE,
    Rechnungsdatum  DATE NOT NULL,
    FristDatum      DATE,                     -- fälligkeitsdatum
    KundeId         INTEGER NOT NULL REFERENCES tbl_Kunde(Id),
    GesamtbetragNetto REAL NOT NULL DEFAULT 0,
    UmsatzsteuerBetrag REAL NOT NULL DEFAULT 0,
    GesamtsteuerRate REAL NOT NULL DEFAULT 0,
    GesamtbetragBrutto REAL NOT NULL DEFAULT 0,
    Status          TEXT NOT NULL DEFAULT 'entwurf', -- entwurf, gesendet, bezahlt, storniert
    Bemerkung       TEXT,
    PDF_Pfad        TEXT,                     -- relativer Pfad zur generierten PDF
    UBL_Pfad        TEXT,                     -- relativer Pfad zum UBL-XML
    ErstelltAm      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    GeandertAm      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexe
CREATE INDEX idx_rechnung_kunde ON tbl_Rechnung(KundeId);
CREATE INDEX idx_rechnung_datum ON tbl_Rechnung(ErstelltAm DESC);
CREATE INDEX idx_rechnung_status ON tbl_Rechnung(Status);
```

### tbl_RechnungsPosition (Positionszeilen der Rechnung)

```sql
CREATE TABLE tbl_RechnungsPosition (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    RechnungId      INTEGER NOT NULL REFERENCES tbl_Rechnung(Id) ON DELETE CASCADE,
    Beschreibung    TEXT NOT NULL,
    Menge           REAL NOT NULL DEFAULT 1,
    Einheit         TEXT NOT NULL DEFAULT 'ST',  -- ST, EUR, Tage, Stunden, km
    EinzelpreisNetto REAL NOT NULL,
    Steuersatz      REAL NOT NULL DEFAULT 19,    -- 0, 7, 19, 25
    GesamtpreisNetto REAL NOT NULL,
    Bestellnr       INTEGER,
    Reihenfolge     INTEGER NOT NULL DEFAULT 1   -- Reihenfolge in der PDF
);

-- Index
CREATE INDEX idx_pos_rechnung ON tbl_RechnungsPosition(RechnungId);
```

### tbl_Kunde (Kundenstamm)

```sql
CREATE TABLE tbl_Kunde (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT NOT NULL,
    Firmenname      TEXT,
    Strasse         TEXT,
    PLZ             TEXT,
    Ort             TEXT,
    Land            TEXT NOT NULL DEFAULT 'DE',
    Email           TEXT,
    Telefon         TEXT,
    UStIdNr         TEXT,                     -- Umsatzsteuer-Identifikationsnummer
    Bemerkung       TEXT,
    ErstelltAm      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexe
CREATE INDEX idx_kunde_name ON tbl_Kunde(Firmenname COLLATE NOCASE);
CREATE INDEX idx_kunde_email ON tbl_Kunde(Email COLLATE NOCASE);
```

### tbl_Vorlage (Rechnungsvorlagen)

```sql
CREATE TABLE tbl_Vorlage (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT NOT NULL,              -- z.B. "Standard", "Projekt", "Tagessatz"
    Typ             TEXT NOT NULL DEFAULT 'frei', -- 'fix' oder 'frei'
    Inhalt          TEXT NOT NULL,              -- JSON mit Positionsbasisdaten oder XML-Vorlage
    IstStandard     INTEGER NOT NULL DEFAULT 0,  -- 1 = Standardvorlage
    ErstelltAm      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### tbl_Einstellungen (Anwendungseinstellungen)

```sql
CREATE TABLE tbl_Einstellungen (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Schlssel        TEXT NOT NULL UNIQUE,       -- z.B. 'unternehmen.name'
    Wert            TEXT,                       -- z.B. 'Meine GmbH'
    AktualisiertAm  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Beispielwerte
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.name', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.strasse', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.plz', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.ort', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.email', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.iban', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('unternehmen.usridnr', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('rechnung.naechste_nr', '1');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('rechnung.prefix', '');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('app.letzte_version', '0.0.0');
INSERT INTO tbl_Einstellungen (Schlssel, Wert) VALUES ('app.auto_update', 'true');
```

## Backup-Strategie

| Aktion                          | Zeitplan                | Pfad                                          |
|---------------------------------|-------------------------|-----------------------------------------------|
| Automatisch (täglich)           | Bei Start des Programms | `%LOCALAPPDATA%/ERechnung-SD/backups/erech_MMDDHHMM.db.bak` |
| Manuell (Benutzer klickt)       | Bei Bedarf              | `erech_manual_MMDDHHMM.db.bak`                |
| Vor Update                      | Bevor Update installiert wird | `erech_preupdate_MMDDHHMM.db.bak`     |

### Backup-Wiederherstellung
1. Benutzer wählt Backup-Datei im Dialog.
2. Programm prüft Gültigkeit der SQLite-Datei (`PRAGMA integrity_check`).
3. Falls gültig → bestehende DB wird durch Backup ersetzt.
4. Programm neustartet automatisch.

## Migrationen

```csharp
// Beispiel für zukünftige Migration
// src/ERechnung.Data/Migrations/20240801_Initial.cs

public class Migration001 : IMigration
{
    public int Version => 1;

    public void Apply(SQLiteConnection conn)
    {
        // Erstellt alle Tabellen (Initial)
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS tbl_Kunde (...);
            CREATE TABLE IF NOT EXISTS tbl_Rechnung (...);
            ...
        ");
    }
}
```

Jede Migration hat eine `Version` und wird automatisch angewendet, wenn die aktuelle DB-Version hinterherhinkt.