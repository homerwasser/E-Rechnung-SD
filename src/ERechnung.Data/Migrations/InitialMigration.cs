namespace ERechnung.Data.Migrations;

internal static class InitialMigration
{
    public const int Version = 1;
    public const string Name = "Initiales Datenbankschema";

    public const string Sql = """
        CREATE TABLE IF NOT EXISTS tbl_Kunde (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Firmenname TEXT NOT NULL CHECK(length(trim(Firmenname)) > 0),
            Ansprechpartner TEXT NOT NULL DEFAULT '',
            Strasse TEXT NOT NULL DEFAULT '',
            PLZ TEXT NOT NULL DEFAULT '',
            Ort TEXT NOT NULL DEFAULT '',
            Land TEXT NOT NULL DEFAULT 'DE',
            Email TEXT NOT NULL DEFAULT '',
            Telefon TEXT NOT NULL DEFAULT '',
            UstIdNr TEXT,
            Bemerkung TEXT NOT NULL DEFAULT '',
            ErstelltAm TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        );

        CREATE INDEX IF NOT EXISTS idx_kunde_firmenname
            ON tbl_Kunde(Firmenname COLLATE NOCASE);
        CREATE INDEX IF NOT EXISTS idx_kunde_email
            ON tbl_Kunde(Email COLLATE NOCASE);

        CREATE TABLE IF NOT EXISTS tbl_FirmaProfil (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
            LogoPfad TEXT NOT NULL DEFAULT '',
            Ansprechpartner TEXT NOT NULL DEFAULT '',
            Strasse TEXT NOT NULL DEFAULT '',
            PLZ TEXT NOT NULL DEFAULT '',
            Ort TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Telefon TEXT NOT NULL DEFAULT '',
            IBAN TEXT NOT NULL DEFAULT '',
            BIC TEXT NOT NULL DEFAULT '',
            UstIdNr TEXT
        );

        CREATE TABLE IF NOT EXISTS tbl_Rechnung (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nummer TEXT NOT NULL UNIQUE,
            Titel TEXT NOT NULL DEFAULT '',
            Erstellungsdatum TEXT NOT NULL DEFAULT CURRENT_DATE,
            Rechnungsdatum TEXT NOT NULL,
            Faelligkeitsdatum TEXT,
            KundeId INTEGER NOT NULL,
            FirmaProfilId INTEGER NOT NULL,
            GesamtbetragNetto NUMERIC NOT NULL DEFAULT 0,
            UmsatzsteuerBetrag NUMERIC NOT NULL DEFAULT 0,
            GesamtsteuerRate NUMERIC NOT NULL DEFAULT 0,
            GesamtbetragBrutto NUMERIC NOT NULL DEFAULT 0,
            Status TEXT NOT NULL DEFAULT 'erstellt'
                CHECK(Status IN ('erstellt', 'versendet', 'offen', 'bezahlt', 'inklaerung', 'storniert')),
            Bemerkung TEXT NOT NULL DEFAULT '',
            ErstelltAm TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            GeaendertAm TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (KundeId) REFERENCES tbl_Kunde(Id) ON DELETE RESTRICT,
            FOREIGN KEY (FirmaProfilId) REFERENCES tbl_FirmaProfil(Id) ON DELETE RESTRICT
        );

        CREATE INDEX IF NOT EXISTS idx_rechnung_kunde ON tbl_Rechnung(KundeId);
        CREATE INDEX IF NOT EXISTS idx_rechnung_status ON tbl_Rechnung(Status);
        CREATE INDEX IF NOT EXISTS idx_rechnung_datum ON tbl_Rechnung(Rechnungsdatum DESC);

        CREATE TABLE IF NOT EXISTS tbl_RechnungsPosition (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RechnungId INTEGER NOT NULL,
            Beschreibung TEXT NOT NULL CHECK(length(trim(Beschreibung)) > 0),
            Menge NUMERIC NOT NULL DEFAULT 1 CHECK(Menge > 0),
            Einheit TEXT NOT NULL DEFAULT 'ST',
            EinzelpreisNetto NUMERIC NOT NULL,
            Steuersatz NUMERIC NOT NULL DEFAULT 19,
            Reihenfolge INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (RechnungId) REFERENCES tbl_Rechnung(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS idx_position_rechnung
            ON tbl_RechnungsPosition(RechnungId);
        """;
}
