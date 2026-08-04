namespace ERechnung.Data.Migrations;

internal static class AddInvoicePdfDataMigration
{
    public const int Version = 3;
    public const string Name = "Leistungsdatum, Logo-Snapshot und PDF-Verknüpfung";

    public const string Sql = """
        ALTER TABLE tbl_Rechnung
            ADD COLUMN Leistungsdatum TEXT NULL;

        ALTER TABLE tbl_Rechnung
            ADD COLUMN PdfRelativerPfad TEXT NULL
                CHECK(PdfRelativerPfad IS NULL OR length(trim(PdfRelativerPfad)) > 0);
        ALTER TABLE tbl_Rechnung
            ADD COLUMN PdfErstelltAm TEXT NULL;
        ALTER TABLE tbl_Rechnung
            ADD COLUMN PdfRechnungsstandAm TEXT NULL;

        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotLogoInhalt BLOB NULL;
        ALTER TABLE tbl_Rechnung
            ADD COLUMN AbsenderSnapshotLogoMedientyp TEXT NULL
                CHECK(
                    AbsenderSnapshotLogoMedientyp IS NULL
                    OR AbsenderSnapshotLogoMedientyp IN (
                        'image/png',
                        'image/jpeg',
                        'image/webp',
                        'image/bmp'
                    )
                );
        """;
}
