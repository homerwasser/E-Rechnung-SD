using ERechnung.Data.Migrations;

namespace ERechnung.Tests.Integration;

public sealed class DatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsync_CreatesSchemaAndRecordsVersionOne()
    {
        using var database = new TemporarySqliteDatabase();

        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name IN (
                  'SchemaMigrations',
                  'tbl_Kunde',
                  'tbl_FirmaProfil',
                  'tbl_Rechnung',
                  'tbl_RechnungsPosition'
              )
            ORDER BY name;
            """;

        var tableNames = new List<string>();
        await using (var reader = await tableCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        Assert.Equal(
            new[]
            {
                "SchemaMigrations",
                "tbl_FirmaProfil",
                "tbl_Kunde",
                "tbl_Rechnung",
                "tbl_RechnungsPosition"
            },
            tableNames);

        await using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 1;";
        var versionOneCount = Convert.ToInt64(await versionCommand.ExecuteScalarAsync());

        Assert.Equal(1, versionOneCount);
    }

    [Fact]
    public async Task MigrateAsync_WhenRunRepeatedly_IsIdempotent()
    {
        using var database = new TemporarySqliteDatabase();

        await DatabaseMigrator.MigrateAsync(database.ConnectionString);
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM SchemaMigrations
            WHERE Version = 1;
            """;
        var versionOneCount = Convert.ToInt64(await command.ExecuteScalarAsync());

        command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
        var migrationCount = Convert.ToInt64(await command.ExecuteScalarAsync());

        Assert.Equal(1, versionOneCount);
        Assert.Equal(1, migrationCount);
    }
}
