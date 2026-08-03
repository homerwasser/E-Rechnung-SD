using Microsoft.Data.Sqlite;

namespace ERechnung.Tests.Integration;

internal sealed class TemporarySqliteDatabase : IDisposable
{
    private readonly string _directoryPath;
    private bool _disposed;

    public TemporarySqliteDatabase()
    {
        _directoryPath = Path.Combine(
            Path.GetTempPath(),
            "ERechnung.Tests.Integration",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directoryPath);

        var databasePath = Path.Combine(_directoryPath, "test.sqlite");
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 30
        }.ToString();
    }

    public string ConnectionString { get; }

    public async Task<SqliteConnection> OpenConnectionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var connection = new SqliteConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DeleteDirectoryWithRetry();
    }

    private void DeleteDirectoryWithRetry()
    {
        const int maxAttempts = 5;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(_directoryPath))
                {
                    Directory.Delete(_directoryPath, recursive: true);
                }

                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastException = exception;
                Thread.Sleep(TimeSpan.FromMilliseconds(50 * attempt));
            }
        }

        throw new IOException(
            $"Das temporäre Testverzeichnis '{_directoryPath}' konnte nicht gelöscht werden.",
            lastException);
    }
}
