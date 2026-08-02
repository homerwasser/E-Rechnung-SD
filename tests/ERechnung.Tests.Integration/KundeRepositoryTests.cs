using ERechnung.Core.Models;
using ERechnung.Data.Migrations;
using ERechnung.Data.Repositories;

namespace ERechnung.Tests.Integration;

public sealed class KundeRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ReturnsIdAndPersistsEveryField()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new KundeRepository(database.ConnectionString);
        var kunde = CreateCompleteCustomer();

        var created = await repository.CreateAsync(kunde);

        Assert.True(created.Id.HasValue);
        Assert.True(created.Id.Value > 0);

        var persisted = await repository.GetByIdAsync(created.Id.Value);
        Assert.NotNull(persisted);
        AssertCustomerEqual(created, persisted);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAllEditableFields()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new KundeRepository(database.ConnectionString);
        var kunde = await repository.CreateAsync(CreateCompleteCustomer());

        kunde.Firmenname = "Aktualisierte AG";
        kunde.Ansprechpartner = "Max Aktualisiert";
        kunde.Strasse = "Neue Straße 12";
        kunde.PLZ = "54321";
        kunde.Ort = "Neustadt";
        kunde.Land = "AT";
        kunde.Email = "max@aktualisiert.example";
        kunde.Telefon = "+43 1 5551234";
        kunde.UstIdNr = "ATU12345678";
        kunde.Bemerkung = "Aktualisierte Stammdaten";

        await repository.UpdateAsync(kunde);

        var persisted = await repository.GetByIdAsync(kunde.Id!.Value);
        Assert.NotNull(persisted);
        AssertCustomerEqual(kunde, persisted);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCustomer()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new KundeRepository(database.ConnectionString);
        var kunde = await repository.CreateAsync(CreateCompleteCustomer());

        await repository.DeleteAsync(kunde.Id!.Value);

        Assert.Null(await repository.GetByIdAsync(kunde.Id.Value));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task GetAllAsync_SortsCompanyNamesAlphabeticallyIgnoringCase()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new KundeRepository(database.ConnectionString);
        await repository.CreateAsync(CreateCompleteCustomer("zeta GmbH"));
        await repository.CreateAsync(CreateCompleteCustomer("Beta GmbH"));
        await repository.CreateAsync(CreateCompleteCustomer("alpha GmbH"));

        var kunden = await repository.GetAllAsync();

        Assert.Equal(
            new[] { "alpha GmbH", "Beta GmbH", "zeta GmbH" },
            kunden.Select(kunde => kunde.Firmenname));
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

    private static Kunde CreateCompleteCustomer(string firmenname = "Muster GmbH")
    {
        return new Kunde
        {
            Firmenname = firmenname,
            Ansprechpartner = "Erika Muster",
            Strasse = "Musterweg 7",
            PLZ = "12345",
            Ort = "Musterstadt",
            Land = "DE",
            Email = "erika@muster.example",
            Telefon = "+49 30 123456",
            UstIdNr = "DE123456789",
            Bemerkung = "Testkunde für Integrationstests",
            ErstelltAm = new DateTime(2026, 7, 15, 10, 30, 0, DateTimeKind.Utc)
        };
    }

    private static void AssertCustomerEqual(Kunde expected, Kunde actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Firmenname, actual.Firmenname);
        Assert.Equal(expected.Ansprechpartner, actual.Ansprechpartner);
        Assert.Equal(expected.Strasse, actual.Strasse);
        Assert.Equal(expected.PLZ, actual.PLZ);
        Assert.Equal(expected.Ort, actual.Ort);
        Assert.Equal(expected.Land, actual.Land);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Telefon, actual.Telefon);
        Assert.Equal(expected.UstIdNr, actual.UstIdNr);
        Assert.Equal(expected.Bemerkung, actual.Bemerkung);
        Assert.Equal(expected.ErstelltAm, actual.ErstelltAm);
    }
}
