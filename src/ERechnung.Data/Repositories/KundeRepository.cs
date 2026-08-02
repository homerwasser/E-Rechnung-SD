using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Repositories;

public sealed class KundeRepository : IRepository<Kunde>
{
    private readonly string _connectionString;

    public KundeRepository(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("Die Verbindungszeichenfolge darf nicht leer sein.", nameof(connectionString))
            : connectionString;
    }

    public async Task<List<Kunde>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        var kunden = await connection.QueryAsync<Kunde>("""
            SELECT Id, Firmenname, Ansprechpartner, Strasse, PLZ, Ort, Land,
                   Email, Telefon, UstIdNr, Bemerkung, ErstelltAm
            FROM tbl_Kunde
            ORDER BY Firmenname COLLATE NOCASE, Ansprechpartner COLLATE NOCASE;
            """);
        return kunden.AsList();
    }

    public async Task<Kunde?> GetByIdAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Kunde>("""
            SELECT Id, Firmenname, Ansprechpartner, Strasse, PLZ, Ort, Land,
                   Email, Telefon, UstIdNr, Bemerkung, ErstelltAm
            FROM tbl_Kunde
            WHERE Id = @Id;
            """, new { Id = id });
    }

    public async Task<Kunde> CreateAsync(Kunde kunde)
    {
        ArgumentNullException.ThrowIfNull(kunde);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var id = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tbl_Kunde (
                    Firmenname, Ansprechpartner, Strasse, PLZ, Ort, Land,
                    Email, Telefon, UstIdNr, Bemerkung, ErstelltAm
                )
                VALUES (
                    @Firmenname, @Ansprechpartner, @Strasse, @PLZ, @Ort, @Land,
                    @Email, @Telefon, @UstIdNr, @Bemerkung, @ErstelltAm
                );
                SELECT last_insert_rowid();
                """, kunde, transaction);

            kunde.Id = checked((int)id);
            await transaction.CommitAsync();
            return kunde;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(Kunde kunde)
    {
        ArgumentNullException.ThrowIfNull(kunde);
        if (kunde.Id is null)
        {
            throw new ArgumentException("Ein Kunde ohne ID kann nicht aktualisiert werden.", nameof(kunde));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var affectedRows = await connection.ExecuteAsync("""
                UPDATE tbl_Kunde
                SET Firmenname = @Firmenname,
                    Ansprechpartner = @Ansprechpartner,
                    Strasse = @Strasse,
                    PLZ = @PLZ,
                    Ort = @Ort,
                    Land = @Land,
                    Email = @Email,
                    Telefon = @Telefon,
                    UstIdNr = @UstIdNr,
                    Bemerkung = @Bemerkung
                WHERE Id = @Id;
                """, kunde, transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException($"Der Kunde mit der ID {kunde.Id} wurde nicht gefunden.");
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
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var affectedRows = await connection.ExecuteAsync(
                "DELETE FROM tbl_Kunde WHERE Id = @Id;",
                new { Id = id },
                transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException($"Der Kunde mit der ID {id} wurde nicht gefunden.");
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
