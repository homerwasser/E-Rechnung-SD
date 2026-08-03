using ERechnung.Data.Migrations;
using Microsoft.Data.Sqlite;

namespace ERechnung.Tests.Integration;

public sealed class DatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsync_OnFreshDatabase_CreatesCompleteSchemaAndBothVersions()
    {
        using var database = new TemporarySqliteDatabase();

        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        var tableNames = await ReadStringsAsync(connection, """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name IN (
                  'SchemaMigrations',
                  'tbl_Kunde',
                  'tbl_FirmaProfil',
                  'tbl_Rechnung',
                  'tbl_RechnungsPosition',
                  'tbl_RechnungsnummerSequenz'
              )
            ORDER BY name;
            """);
        var versions = await ReadInt64ValuesAsync(connection, """
            SELECT Version
            FROM SchemaMigrations
            ORDER BY Version;
            """);

        Assert.Equal(
            new[]
            {
                "SchemaMigrations",
                "tbl_FirmaProfil",
                "tbl_Kunde",
                "tbl_Rechnung",
                "tbl_RechnungsPosition",
                "tbl_RechnungsnummerSequenz"
            },
            tableNames);
        Assert.Equal(new long[] { 1, 2 }, versions);
    }

    [Fact]
    public async Task MigrateAsync_FromVersionOne_BackfillsSnapshotsAndSequence()
    {
        using var database = new TemporarySqliteDatabase();
        await CreateVersionOneDatabaseAsync(database);

        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Waehrung,
                   EmpfaengerSnapshotName,
                   EmpfaengerSnapshotAnsprechpartner,
                   EmpfaengerSnapshotStrasse,
                   EmpfaengerSnapshotPLZ,
                   EmpfaengerSnapshotOrt,
                   EmpfaengerSnapshotLand,
                   EmpfaengerSnapshotEmail,
                   EmpfaengerSnapshotUstIdNr,
                   AbsenderSnapshotName,
                   AbsenderSnapshotLogoPfad,
                   AbsenderSnapshotAnsprechpartner,
                   AbsenderSnapshotStrasse,
                   AbsenderSnapshotPLZ,
                   AbsenderSnapshotOrt,
                   AbsenderSnapshotLand,
                   AbsenderSnapshotEmail,
                   AbsenderSnapshotTelefon,
                   AbsenderSnapshotUstIdNr,
                   AbsenderSnapshotIBAN,
                   AbsenderSnapshotBIC
            FROM tbl_Rechnung
            WHERE Nummer = '2025-042';
            """;

        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal("EUR", reader.GetString(0));
            Assert.Equal("Altbestand Kunde GmbH", reader.GetString(1));
            Assert.Equal("Kai Kunde", reader.GetString(2));
            Assert.Equal("Kundenweg 3", reader.GetString(3));
            Assert.Equal("12345", reader.GetString(4));
            Assert.Equal("Kundenstadt", reader.GetString(5));
            Assert.Equal("AT", reader.GetString(6));
            Assert.Equal("kunde@altbestand.example", reader.GetString(7));
            Assert.Equal("ATU12345678", reader.GetString(8));
            Assert.Equal("Altbestand Absender GmbH", reader.GetString(9));
            Assert.Equal("logos/altbestand.png", reader.GetString(10));
            Assert.Equal("Ada Absender", reader.GetString(11));
            Assert.Equal("Absenderallee 8", reader.GetString(12));
            Assert.Equal("54321", reader.GetString(13));
            Assert.Equal("Absenderstadt", reader.GetString(14));
            Assert.Equal("DE", reader.GetString(15));
            Assert.Equal("absender@altbestand.example", reader.GetString(16));
            Assert.Equal("+49 30 555000", reader.GetString(17));
            Assert.Equal("DE123456789", reader.GetString(18));
            Assert.Equal("DE02120300000000202051", reader.GetString(19));
            Assert.Equal("BYLADEM1001", reader.GetString(20));
        }

        command.CommandText = "SELECT Land FROM tbl_FirmaProfil WHERE Id = 1;";
        Assert.Equal("DE", Convert.ToString(await command.ExecuteScalarAsync()));

        command.CommandText = """
            SELECT LetzteNummer
            FROM tbl_RechnungsnummerSequenz
            WHERE Jahr = 2025;
            """;
        Assert.Equal(42L, Convert.ToInt64(await command.ExecuteScalarAsync()));

        command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
        Assert.Equal(2L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task MigrateAsync_WhenRunRepeatedly_IsIdempotent()
    {
        using var database = new TemporarySqliteDatabase();

        await DatabaseMigrator.MigrateAsync(database.ConnectionString);
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        var versions = await ReadInt64ValuesAsync(connection, """
            SELECT Version
            FROM SchemaMigrations
            ORDER BY Version;
            """);
        var rechnungColumns = await ReadTableColumnsAsync(connection, "tbl_Rechnung");

        Assert.Equal(new long[] { 1, 2 }, versions);
        Assert.Equal(1, rechnungColumns.Count(column => column.Name == "Waehrung"));
        Assert.Equal(1, rechnungColumns.Count(column => column.Name == "EmpfaengerSnapshotName"));
        Assert.Equal(1, rechnungColumns.Count(column => column.Name == "AbsenderSnapshotName"));
    }

    [Fact]
    public async Task MigrateAsync_WhenStartedConcurrently_AppliesEveryVersionExactlyOnce()
    {
        using var database = new TemporarySqliteDatabase();

        await Task.WhenAll(
            DatabaseMigrator.MigrateAsync(database.ConnectionString),
            DatabaseMigrator.MigrateAsync(database.ConnectionString));

        await using var connection = await database.OpenConnectionAsync();
        var migrationCounts = await ReadStringsAsync(connection, """
            SELECT CAST(Version AS TEXT) || ':' || CAST(COUNT(*) AS TEXT)
            FROM SchemaMigrations
            GROUP BY Version
            ORDER BY Version;
            """);

        Assert.Equal(new[] { "1:1", "2:1" }, migrationCounts);
    }

    [Fact]
    public async Task MigrateAsync_VersionTwoSchema_HasExpectedColumnsDefaultsAndChecks()
    {
        using var database = new TemporarySqliteDatabase();
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        var firmaColumns = await ReadTableColumnsAsync(connection, "tbl_FirmaProfil");
        var rechnungColumns = await ReadTableColumnsAsync(connection, "tbl_Rechnung");
        var sequenceColumns = await ReadTableColumnsAsync(connection, "tbl_RechnungsnummerSequenz");

        AssertColumn(firmaColumns, "Land", notNull: true, defaultValue: "'DE'");
        AssertColumn(rechnungColumns, "Waehrung", notNull: true, defaultValue: "'EUR'");

        var expectedSnapshotColumns = new[]
        {
            "EmpfaengerSnapshotName",
            "EmpfaengerSnapshotAnsprechpartner",
            "EmpfaengerSnapshotStrasse",
            "EmpfaengerSnapshotPLZ",
            "EmpfaengerSnapshotOrt",
            "EmpfaengerSnapshotLand",
            "EmpfaengerSnapshotEmail",
            "EmpfaengerSnapshotUstIdNr",
            "AbsenderSnapshotName",
            "AbsenderSnapshotLogoPfad",
            "AbsenderSnapshotAnsprechpartner",
            "AbsenderSnapshotStrasse",
            "AbsenderSnapshotPLZ",
            "AbsenderSnapshotOrt",
            "AbsenderSnapshotLand",
            "AbsenderSnapshotEmail",
            "AbsenderSnapshotTelefon",
            "AbsenderSnapshotUstIdNr",
            "AbsenderSnapshotIBAN",
            "AbsenderSnapshotBIC"
        };
        Assert.All(
            expectedSnapshotColumns,
            expected => Assert.Contains(rechnungColumns, column => column.Name == expected));

        Assert.Contains(sequenceColumns, column => column.Name == "Jahr" && column.PrimaryKey);
        AssertColumn(sequenceColumns, "LetzteNummer", notNull: true, defaultValue: "0");

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type = 'table' AND name = 'tbl_RechnungsnummerSequenz';
            """;
        var sequenceSql = Convert.ToString(await command.ExecuteScalarAsync());
        Assert.Contains("CHECK", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LetzteNummer >= 0", sequenceSql, StringComparison.Ordinal);

        command.CommandText = """
            INSERT INTO tbl_FirmaProfil (Name, Land)
            VALUES ('Ungültiges Länderprofil', 'DÄ');
            """;
        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal(19, exception.SqliteErrorCode);

        command.CommandText = """
            INSERT INTO tbl_FirmaProfil (Name)
            VALUES ('Profil mit Standardland');
            """;
        await command.ExecuteNonQueryAsync();
        command.CommandText = """
            SELECT Land
            FROM tbl_FirmaProfil
            WHERE Name = 'Profil mit Standardland';
            """;
        Assert.Equal("DE", Convert.ToString(await command.ExecuteScalarAsync()));
    }

    private static async Task CreateVersionOneDatabaseAsync(TemporarySqliteDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE SchemaMigrations (
                Version INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            INSERT INTO SchemaMigrations (Version, Name)
            VALUES (1, 'Initiales Datenbankschema');

            CREATE TABLE tbl_Kunde (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Firmenname TEXT NOT NULL,
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

            CREATE TABLE tbl_FirmaProfil (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
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

            CREATE TABLE tbl_Rechnung (
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
                Status TEXT NOT NULL DEFAULT 'erstellt',
                Bemerkung TEXT NOT NULL DEFAULT '',
                ErstelltAm TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                GeaendertAm TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (KundeId) REFERENCES tbl_Kunde(Id) ON DELETE RESTRICT,
                FOREIGN KEY (FirmaProfilId) REFERENCES tbl_FirmaProfil(Id) ON DELETE RESTRICT
            );

            CREATE TABLE tbl_RechnungsPosition (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RechnungId INTEGER NOT NULL,
                Beschreibung TEXT NOT NULL,
                Menge NUMERIC NOT NULL DEFAULT 1,
                Einheit TEXT NOT NULL DEFAULT 'ST',
                EinzelpreisNetto NUMERIC NOT NULL,
                Steuersatz NUMERIC NOT NULL DEFAULT 19,
                Reihenfolge INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (RechnungId) REFERENCES tbl_Rechnung(Id) ON DELETE CASCADE
            );

            INSERT INTO tbl_Kunde (
                Id, Firmenname, Ansprechpartner, Strasse, PLZ, Ort, Land,
                Email, Telefon, UstIdNr, Bemerkung
            )
            VALUES (
                1, 'Altbestand Kunde GmbH', 'Kai Kunde', 'Kundenweg 3', '12345',
                'Kundenstadt', 'AT', 'kunde@altbestand.example', '+43 1 555000',
                'ATU12345678', 'Synthetischer Altbestand'
            );

            INSERT INTO tbl_FirmaProfil (
                Id, Name, LogoPfad, Ansprechpartner, Strasse, PLZ, Ort,
                Email, Telefon, IBAN, BIC, UstIdNr
            )
            VALUES (
                1, 'Altbestand Absender GmbH', 'logos/altbestand.png', 'Ada Absender',
                'Absenderallee 8', '54321', 'Absenderstadt',
                'absender@altbestand.example', '+49 30 555000',
                'DE02120300000000202051', 'BYLADEM1001', 'DE123456789'
            );

            INSERT INTO tbl_Rechnung (
                Nummer, Titel, Rechnungsdatum, KundeId, FirmaProfilId
            )
            VALUES
                ('2025-007', 'Altbestand 7', '2025-01-10', 1, 1),
                ('2025-042', 'Altbestand 42', '2025-02-10', 1, 1),
                ('FREIFORM', 'Nicht sequenziell', '2025-03-10', 1, 1);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ReadStringsAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<List<long>> ReadInt64ValuesAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<long>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetInt64(0));
        }

        return values;
    }

    private static async Task<List<TableColumn>> ReadTableColumnsAsync(
        SqliteConnection connection,
        string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{tableName}');";
        var columns = new List<TableColumn>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new TableColumn(
                reader.GetString(1),
                reader.GetInt64(3) == 1,
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt64(5) == 1));
        }

        return columns;
    }

    private static void AssertColumn(
        IEnumerable<TableColumn> columns,
        string name,
        bool notNull,
        string defaultValue)
    {
        var column = Assert.Single(columns, column => column.Name == name);
        Assert.Equal(notNull, column.NotNull);
        Assert.Equal(defaultValue, column.DefaultValue);
    }

    private sealed record TableColumn(
        string Name,
        bool NotNull,
        string? DefaultValue,
        bool PrimaryKey);
}
