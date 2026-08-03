using ERechnung.Core.Models;
using ERechnung.Data.Migrations;
using ERechnung.Data.Repositories;

namespace ERechnung.Tests.Integration;

public sealed class FirmaProfilRepositoryTests
{
    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FirmaProfilRepository("  "));
    }

    [Fact]
    public async Task CreateAsync_ReturnsIdAndPersistsEveryField()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new FirmaProfilRepository(database.ConnectionString);
        var firmaProfil = CreateCompleteProfile();

        var created = await repository.CreateAsync(firmaProfil);

        Assert.Same(firmaProfil, created);
        Assert.True(created.Id is > 0);

        var persisted = await repository.GetByIdAsync(created.Id!.Value);
        Assert.NotNull(persisted);
        AssertProfileEqual(created, persisted);
    }

    [Fact]
    public async Task UpdateAsync_PersistsEveryEditableField()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new FirmaProfilRepository(database.ConnectionString);
        var firmaProfil = await repository.CreateAsync(CreateCompleteProfile());

        firmaProfil.Name = "Aktualisierte Absender AG";
        firmaProfil.LogoPfad = "logos/aktualisiert.svg";
        firmaProfil.Ansprechpartner = "Alex Aktualisiert";
        firmaProfil.Strasse = "Neue Allee 20";
        firmaProfil.PLZ = "1010";
        firmaProfil.Ort = "Wien";
        firmaProfil.Land = "AT";
        firmaProfil.Email = "aktualisiert@absender.example";
        firmaProfil.Telefon = "+43 1 5551234";
        firmaProfil.IBAN = "AT611904300234573201";
        firmaProfil.BIC = "BKAUATWW";
        firmaProfil.UstIdNr = "ATU98765432";

        await repository.UpdateAsync(firmaProfil);

        var persisted = await repository.GetByIdAsync(firmaProfil.Id!.Value);
        Assert.NotNull(persisted);
        AssertProfileEqual(firmaProfil, persisted);
    }

    [Fact]
    public async Task GetAllAsync_SortsNamesAlphabeticallyIgnoringCase()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new FirmaProfilRepository(database.ConnectionString);
        await repository.CreateAsync(CreateCompleteProfile("zeta GmbH"));
        await repository.CreateAsync(CreateCompleteProfile("Beta GmbH"));
        await repository.CreateAsync(CreateCompleteProfile("alpha GmbH"));

        var profile = await repository.GetAllAsync();

        Assert.Equal(
            new[] { "alpha GmbH", "Beta GmbH", "zeta GmbH" },
            profile.Select(firmaProfil => firmaProfil.Name));
    }

    [Fact]
    public async Task DeleteAsync_RemovesUnreferencedProfile()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new FirmaProfilRepository(database.ConnectionString);
        var firmaProfil = await repository.CreateAsync(CreateCompleteProfile());

        await repository.DeleteAsync(firmaProfil.Id!.Value);

        Assert.Null(await repository.GetByIdAsync(firmaProfil.Id.Value));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task MissingRecords_AreReported()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new FirmaProfilRepository(database.ConnectionString);
        var missing = CreateCompleteProfile();
        missing.Id = 9876;

        Assert.Null(await repository.GetByIdAsync(missing.Id.Value));

        var updateException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(missing));
        var deleteException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.DeleteAsync(missing.Id.Value));

        Assert.Contains("9876", updateException.Message, StringComparison.Ordinal);
        Assert.Contains("9876", deleteException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_WhenProfileIsReferenced_ReturnsUnderstandableError()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var firmaRepository = new FirmaProfilRepository(database.ConnectionString);
        var kundenRepository = new KundeRepository(database.ConnectionString);
        var firmaProfil = await firmaRepository.CreateAsync(CreateCompleteProfile());
        var kunde = await kundenRepository.CreateAsync(new Kunde
        {
            Firmenname = "Referenzkunde GmbH",
            Land = "DE"
        });

        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO tbl_Rechnung (
                    Nummer, Rechnungsdatum, KundeId, FirmaProfilId
                )
                VALUES (
                    '2026-900', '2026-08-03', @KundeId, @FirmaProfilId
                );
                """;
            command.Parameters.AddWithValue("@KundeId", kunde.Id!.Value);
            command.Parameters.AddWithValue("@FirmaProfilId", firmaProfil.Id!.Value);
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => firmaRepository.DeleteAsync(firmaProfil.Id!.Value));

        Assert.Contains("kann nicht gelöscht werden", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Rechnung", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(await firmaRepository.GetByIdAsync(firmaProfil.Id!.Value));
    }

    private static async Task<TemporarySqliteDatabase> CreateMigratedDatabaseAsync()
    {
        var database = new TemporarySqliteDatabase();
        try
        {
            await DatabaseMigrator.MigrateAsync(database.ConnectionString);
            return database;
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    private static FirmaProfil CreateCompleteProfile(string name = "Synthetische Absender GmbH")
    {
        return new FirmaProfil
        {
            Name = name,
            LogoPfad = "logos/synthetisch.png",
            Ansprechpartner = "Fiona Firma",
            Strasse = "Firmenstraße 12",
            PLZ = "12345",
            Ort = "Firmenstadt",
            Land = "DE",
            Email = "fiona@absender.example",
            Telefon = "+49 30 5551234",
            IBAN = "DE02120300000000202051",
            BIC = "BYLADEM1001",
            UstIdNr = "DE123456789"
        };
    }

    private static void AssertProfileEqual(FirmaProfil expected, FirmaProfil actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.LogoPfad, actual.LogoPfad);
        Assert.Equal(expected.Ansprechpartner, actual.Ansprechpartner);
        Assert.Equal(expected.Strasse, actual.Strasse);
        Assert.Equal(expected.PLZ, actual.PLZ);
        Assert.Equal(expected.Ort, actual.Ort);
        Assert.Equal(expected.Land, actual.Land);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Telefon, actual.Telefon);
        Assert.Equal(expected.IBAN, actual.IBAN);
        Assert.Equal(expected.BIC, actual.BIC);
        Assert.Equal(expected.UstIdNr, actual.UstIdNr);
    }
}
