using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Migrations;

public static class DatabaseMigrator
{
    private static readonly IReadOnlyList<MigrationDefinition> Migrations =
    [
        new(InitialMigration.Version, InitialMigration.Name, InitialMigration.Sql)
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

        var appliedVersions = new HashSet<int>(await connection.QueryAsync<int>(
            "SELECT Version FROM SchemaMigrations ORDER BY Version;"));

        foreach (var migration in Migrations)
        {
            if (appliedVersions.Contains(migration.Version))
            {
                continue;
            }

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await connection.ExecuteAsync(migration.Sql, transaction: transaction);
                await connection.ExecuteAsync(
                    "INSERT INTO SchemaMigrations (Version, Name) VALUES (@Version, @Name);",
                    new { migration.Version, migration.Name },
                    transaction);
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
