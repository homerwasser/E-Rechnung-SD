using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data;

public static class DbConnectionHelper
{
    private static string _connectionString = string.Empty;

    public static void Initialize()
    {
        var basePath = Environment.CurrentDirectory;
        var dataDir = Path.Combine(basePath, "data");
        Directory.CreateDirectory(dataDir);
        _connectionString = $"Data Source={Path.Combine(dataDir, "erechnung.db")}";
    }

    public static string ConnectionString => _connectionString;

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}