namespace ERechnung.Data.Migrations;

internal static class AddInvoiceCreationSchemaMigration
{
    public const int Version = 2;
    public const string Name = "Rechnungserstellung mit Snapshots und Nummernsequenzen";

    public const string Sql = """
        ALTER TABLE tbl_FirmaProfil
            ADD COLUMN Land TEXT NOT NULL DEFAULT 'DE'
                CHECK(length(trim(Land)) = 2 AND Land NOT GLOB '*[^A-Za-z]*');

        ALTER TABLE tbl_Rechnung
            ADD COLUMN Waehrung TEXT NOT NULL DEFAULT 'EUR'
                CHECK(length(Waehrung) = 3 AND Waehrung NOT GLOB '*[^A-Za-z]*');

        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotName TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotAnsprechpartner TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotStrasse TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotPLZ TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotOrt TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotLand TEXT NOT NULL DEFAULT 'DE';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotEmail TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN EmpfaengerSnapshotUstIdNr TEXT;

        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotName TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotLogoPfad TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotAnsprechpartner TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotStrasse TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotPLZ TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotOrt TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotLand TEXT NOT NULL DEFAULT 'DE';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotEmail TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotTelefon TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotUstIdNr TEXT;
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotIBAN TEXT NOT NULL DEFAULT '';
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotBIC TEXT NOT NULL DEFAULT '';

        UPDATE tbl_Rechnung
        SET EmpfaengerSnapshotName = COALESCE(
                (SELECT Firmenname FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotAnsprechpartner = COALESCE(
                (SELECT Ansprechpartner FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotStrasse = COALESCE(
                (SELECT Strasse FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotPLZ = COALESCE(
                (SELECT PLZ FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotOrt = COALESCE(
                (SELECT Ort FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotLand = COALESCE(
                NULLIF(trim((SELECT Land FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId)), ''),
                'DE'),
            EmpfaengerSnapshotEmail = COALESCE(
                (SELECT Email FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
                ''),
            EmpfaengerSnapshotUstIdNr = (
                SELECT UstIdNr FROM tbl_Kunde WHERE tbl_Kunde.Id = tbl_Rechnung.KundeId),
            AbsenderSnapshotName = COALESCE(
                (SELECT Name FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotLogoPfad = COALESCE(
                (SELECT LogoPfad FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotAnsprechpartner = COALESCE(
                (SELECT Ansprechpartner FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotStrasse = COALESCE(
                (SELECT Strasse FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotPLZ = COALESCE(
                (SELECT PLZ FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotOrt = COALESCE(
                (SELECT Ort FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotLand = 'DE',
            AbsenderSnapshotEmail = COALESCE(
                (SELECT Email FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotTelefon = COALESCE(
                (SELECT Telefon FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotUstIdNr = (
                SELECT UstIdNr FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
            AbsenderSnapshotIBAN = COALESCE(
                (SELECT IBAN FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                ''),
            AbsenderSnapshotBIC = COALESCE(
                (SELECT BIC FROM tbl_FirmaProfil WHERE tbl_FirmaProfil.Id = tbl_Rechnung.FirmaProfilId),
                '');

        CREATE TABLE tbl_RechnungsnummerSequenz (
            Jahr INTEGER PRIMARY KEY CHECK(Jahr BETWEEN 1 AND 9999),
            LetzteNummer INTEGER NOT NULL DEFAULT 0
                CHECK(typeof(LetzteNummer) = 'integer' AND LetzteNummer >= 0)
        ) WITHOUT ROWID;

        INSERT INTO tbl_RechnungsnummerSequenz (Jahr, LetzteNummer)
        SELECT CAST(substr(Nummer, 1, 4) AS INTEGER),
               MAX(CAST(substr(Nummer, 6) AS INTEGER))
        FROM tbl_Rechnung
        WHERE length(Nummer) BETWEEN 6 AND 23
          AND substr(Nummer, 5, 1) = '-'
          AND substr(Nummer, 1, 4) NOT GLOB '*[^0-9]*'
          AND substr(Nummer, 6) NOT GLOB '*[^0-9]*'
          AND CAST(substr(Nummer, 1, 4) AS INTEGER) BETWEEN 1 AND 9999
        GROUP BY CAST(substr(Nummer, 1, 4) AS INTEGER);
        """;
}
