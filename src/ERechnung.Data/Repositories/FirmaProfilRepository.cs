using Dapper;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Repositories;

public sealed class FirmaProfilRepository : IRepository<FirmaProfil>
{
    private readonly string _connectionString;

    public FirmaProfilRepository(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException(
                "Die Verbindungszeichenfolge darf nicht leer sein.",
                nameof(connectionString))
            : connectionString;
    }

    public async Task<List<FirmaProfil>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var profile = await connection.QueryAsync<FirmaProfil>("""
            SELECT Id, Name, LogoPfad, Ansprechpartner, Strasse, PLZ, Ort, Land,
                   Email, Telefon, IBAN, BIC, UstIdNr
            FROM tbl_FirmaProfil
            ORDER BY Name COLLATE NOCASE, Id;
            """);
        return profile.AsList();
    }

    public async Task<FirmaProfil?> GetByIdAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<FirmaProfil>("""
            SELECT Id, Name, LogoPfad, Ansprechpartner, Strasse, PLZ, Ort, Land,
                   Email, Telefon, IBAN, BIC, UstIdNr
            FROM tbl_FirmaProfil
            WHERE Id = @Id;
            """, new { Id = id });
    }

    public async Task<FirmaProfil> CreateAsync(FirmaProfil firmaProfil)
    {
        ArgumentNullException.ThrowIfNull(firmaProfil);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var id = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tbl_FirmaProfil (
                    Name, LogoPfad, Ansprechpartner, Strasse, PLZ, Ort, Land,
                    Email, Telefon, IBAN, BIC, UstIdNr
                )
                VALUES (
                    @Name, @LogoPfad, @Ansprechpartner, @Strasse, @PLZ, @Ort, @Land,
                    @Email, @Telefon, @IBAN, @BIC, @UstIdNr
                );
                SELECT last_insert_rowid();
                """, firmaProfil, transaction);
            var neueId = checked((int)id);

            await transaction.CommitAsync();
            firmaProfil.Id = neueId;
            return firmaProfil;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(FirmaProfil firmaProfil)
    {
        ArgumentNullException.ThrowIfNull(firmaProfil);
        if (firmaProfil.Id is null)
        {
            throw new ArgumentException(
                "Ein Firmenprofil ohne ID kann nicht aktualisiert werden.",
                nameof(firmaProfil));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var affectedRows = await connection.ExecuteAsync("""
                UPDATE tbl_FirmaProfil
                SET Name = @Name,
                    LogoPfad = @LogoPfad,
                    Ansprechpartner = @Ansprechpartner,
                    Strasse = @Strasse,
                    PLZ = @PLZ,
                    Ort = @Ort,
                    Land = @Land,
                    Email = @Email,
                    Telefon = @Telefon,
                    IBAN = @IBAN,
                    BIC = @BIC,
                    UstIdNr = @UstIdNr
                WHERE Id = @Id;
                """, firmaProfil, transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Das Firmenprofil mit der ID {firmaProfil.Id} wurde nicht gefunden.");
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var wirdVerwendet = await connection.ExecuteScalarAsync<long>("""
                SELECT EXISTS (
                    SELECT 1
                    FROM tbl_Rechnung
                    WHERE FirmaProfilId = @Id
                );
                """, new { Id = id }, transaction);
            if (wirdVerwendet == 1)
            {
                throw new InvalidOperationException(
                    $"Das Firmenprofil mit der ID {id} kann nicht gelöscht werden, "
                    + "weil es von mindestens einer Rechnung verwendet wird.");
            }

            var affectedRows = await connection.ExecuteAsync(
                "DELETE FROM tbl_FirmaProfil WHERE Id = @Id;",
                new { Id = id },
                transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Das Firmenprofil mit der ID {id} wurde nicht gefunden.");
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
