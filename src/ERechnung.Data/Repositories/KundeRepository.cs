using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Data.Repositories;

public class KundeRepository : IRepository<Kunde>
{
    private readonly string _connectionString;

    public KundeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string GetConnectionString() => _connectionString;

    public async Task<List<Kunde>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return (await connection.QueryAsync<Kunde>("SELECT * FROM tbl_Kunde ORDER BY Firmenname")).AsList();
    }

    public async Task<Kunde?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Kunde>("SELECT * FROM tbl_Kunde WHERE Id = @Id", new { Id = id });
    }

    public async Task<Kunde> CreateAsync(Kunde kunde)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO tbl_Kunde (Firmenname, Ansprechpartner, Strasse, PLZ, Ort, Land, Email, Telefon, UstIdNr, ErstelltAm) 
            VALUES (@Firmenname, @Ansprechpartner, @Strasse, @PLZ, @Ort, @Land, @Email, @Telefon, @UstIdNr, @ErstelltAm)
        ", kunde);
        return kunde;
    }

    public async Task UpdateAsync(Kunde kunde)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE tbl_Kunde SET Firmenname=@Firmenname, Ansprechpartner=@Ansprechpartner, 
            Strasse=@Strasse, PLZ=@PLZ, Ort=@Ort, Land=@Land, Email=@Email, Telefon=@Telefon, UstIdNr=@UstIdNr 
            WHERE Id = @Id
        ", kunde);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM tbl_Kunde WHERE Id = @Id", new { Id = id });
    }
}