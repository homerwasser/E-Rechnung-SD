using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Migrations;

public static class DatabaseMigrator
{
    private static readonly IReadOnlyList<MigrationDefinition> Migrations =
    [
        new(InitialMigration.Version, InitialMigration.Name, InitialMigration.Sql),
        new(
            AddInvoiceCreationSchemaMigration.Version,
            AddInvoiceCreationSchemaMigration.Name,
            AddInvoiceCreationSchemaMigration.Sql)
    ];

    public static async Task MigrateAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Version INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        foreach (var migration in Migrations)
        {
            await using var transaction = connection.BeginTransaction(deferred: false);
            try
            {
                var istBereitsAngewendet = await connection.ExecuteScalarAsync<long>("""
                    SELECT COUNT(*)
                    FROM SchemaMigrations
                    WHERE Version = @Version;
                    """, new { migration.Version }, transaction) > 0;

                if (!istBereitsAngewendet)
                {
                    await connection.ExecuteAsync(migration.Sql, transaction: transaction);
                    await connection.ExecuteAsync(
                        "INSERT INTO SchemaMigrations (Version, Name) VALUES (@Version, @Name);",
                        new { migration.Version, migration.Name },
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    private sealed record MigrationDefinition(int Version, string Name, string Sql);
}
