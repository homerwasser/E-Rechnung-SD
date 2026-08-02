using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data;

public static class DbConnectionHelper
{
    private static string _connectionString = string.Empty;

    public static string ConnectionString => !string.IsNullOrWhiteSpace(_connectionString)
        ? _connectionString
        : throw new InvalidOperationException("Die Datenbankverbindung wurde noch nicht initialisiert.");

    public static string DatabasePath { get; private set; } = string.Empty;

    public static void Initialize(string? databasePath = null)
    {
        DatabasePath = Path.GetFullPath(databasePath ?? GetDefaultDatabasePath());

        var dataDirectory = Path.GetDirectoryName(DatabasePath)
            ?? throw new InvalidOperationException("Der Datenbankordner konnte nicht bestimmt werden.");

        Directory.CreateDirectory(dataDirectory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            ForeignKeys = true
        }.ToString();
    }

    public static string GetDefaultDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "ERechnung-SD", "data", "erechnung.db");
    }

    public static SqliteConnection GetConnection() => new(ConnectionString);
}
