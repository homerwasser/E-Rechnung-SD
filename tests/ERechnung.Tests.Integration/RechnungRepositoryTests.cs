using ERechnung.Core.Models;
using ERechnung.Core.Services;
using ERechnung.Data.Migrations;
using ERechnung.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace ERechnung.Tests.Integration;

public sealed class RechnungRepositoryTests
{
    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RechnungRepository(string.Empty));
    }

    [Fact]
    public async Task CreateAndGetByIdAsync_RoundTripsHeaderSnapshotsDatesDecimalsAndPositions()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var rechnung = CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3));
        rechnung.Leistungsdatum = new DateTime(2026, 8, 2);
        rechnung.AbsenderSnapshot!.LogoInhalt = [137, 80, 78, 71, 13, 10, 26, 10];
        rechnung.AbsenderSnapshot.LogoMedientyp = "image/png";
        rechnung.PdfVerknuepfung = new RechnungsPdfVerknuepfung(
            "2026/rechnung-1-2026-001-123.pdf",
            new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 3, 10, 15, 0, DateTimeKind.Utc));
        rechnung.GeaendertAm = rechnung.ErstelltAm.AddDays(1);

        var created = await repository.CreateAsync(rechnung);

        Assert.Same(rechnung, created);
        Assert.True(created.Id is > 0);
        Assert.Equal("2026-001", created.Nummer);
        Assert.Equal(created.ErstelltAm, created.GeaendertAm);
        Assert.Equal(DateTimeKind.Utc, created.ErstelltAm.Kind);
        Assert.All(created.Positionen, position =>
        {
            Assert.True(position.Id is > 0);
            Assert.Equal(created.Id, position.RechnungId);
        });

        var persisted = await repository.GetByIdAsync(created.Id!.Value);

        Assert.NotNull(persisted);
        AssertInvoiceEqual(created, persisted);
        Assert.Equal(new[] { 1, 2 }, persisted.Positionen.Select(position => position.Reihenfolge));
        Assert.Equal(
            created.Positionen.Select(position => position.Id),
            persisted.Positionen.Select(position => position.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllPositionsAndKeepsPersistedNumber()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var rechnung = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id!.Value));
        var altePositionsIds = rechnung.Positionen.Select(position => position.Id!.Value).ToArray();
        var bisherGeaendertAm = rechnung.GeaendertAm;

        rechnung.Nummer = "MANIPULIERT";
        rechnung.Titel = "Aktualisierte synthetische Rechnung";
        rechnung.Bemerkung = "Positionen vollständig ersetzt";
        rechnung.Positionen.RemoveAt(0);
        rechnung.Positionen[0].Beschreibung = "Geänderte Bestandsposition";
        rechnung.Positionen[0].Menge = 4.75m;
        rechnung.Positionen.Add(new RechnungsPosition
        {
            Beschreibung = "Neu hinzugefügte Position",
            Menge = 3.25m,
            Einheit = "STD",
            EinzelpreisNetto = 87.65m,
            Steuersatz = 19m
        });
        RechnungCalculator.Berechnen(rechnung);

        await repository.UpdateAsync(rechnung);

        Assert.Equal("2026-001", rechnung.Nummer);
        Assert.True(rechnung.GeaendertAm > bisherGeaendertAm);
        Assert.Equal(DateTimeKind.Utc, rechnung.GeaendertAm.Kind);
        Assert.Equal(new[] { 1, 2 }, rechnung.Positionen.Select(position => position.Reihenfolge));
        Assert.All(rechnung.Positionen, position =>
        {
            Assert.True(position.Id is > 0);
            Assert.Equal(rechnung.Id, position.RechnungId);
            Assert.DoesNotContain(position.Id!.Value, altePositionsIds);
        });

        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(rechnung.Id!.Value));
        AssertInvoiceEqual(rechnung, persisted);
        Assert.DoesNotContain(
            persisted.Positionen,
            position => position.Id == altePositionsIds[0]);
        Assert.Equal(
            new[] { "Geänderte Bestandsposition", "Neu hinzugefügte Position" },
            persisted.Positionen.Select(position => position.Beschreibung));
    }

    [Fact]
    public async Task UpdateAsync_WithStaleCopy_RejectsHeaderStatusAndPositionOverwrite()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var first = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id!.Value));
        var second = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        var staleTimestamp = second.GeaendertAm;

        Assert.Equal(first.GeaendertAm, staleTimestamp);

        first.Titel = "Erste parallele Bearbeitung";
        first.Status = RechnungsStatus.Offen;
        first.Positionen[0].Beschreibung = "Position aus erster Bearbeitung";
        first.Positionen.RemoveAt(1);
        RechnungCalculator.Berechnen(first);

        second.Titel = "Veraltete parallele Bearbeitung";
        second.Status = RechnungsStatus.Storniert;
        second.Positionen[0].Beschreibung = "Position aus veralteter Bearbeitung";
        second.Positionen.RemoveAt(1);
        RechnungCalculator.Berechnen(second);

        await repository.UpdateAsync(first);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(second));

        Assert.Contains("zwischenzeitlich geändert", exception.Message, StringComparison.Ordinal);
        Assert.Equal(staleTimestamp, second.GeaendertAm);
        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        AssertInvoiceEqual(first, persisted);
        Assert.Equal(RechnungsStatus.Offen, persisted.Status);
        Assert.Equal("Position aus erster Bearbeitung", Assert.Single(persisted.Positionen).Beschreibung);
    }

    [Fact]
    public async Task UpdateAsync_WhenPositionViolatesConstraint_RollsBackHeaderAndPositions()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var before = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id!.Value));
        var update = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        var positionsIdsBefore = update.Positionen.Select(position => position.Id).ToArray();

        update.Titel = "Darf nicht gespeichert werden";
        update.Positionen.Add(new RechnungsPosition
        {
            Beschreibung = "Ungültige Menge",
            Menge = 0m,
            Einheit = "ST",
            EinzelpreisNetto = 10m,
            Steuersatz = 19m,
            Reihenfolge = 3
        });

        await Assert.ThrowsAsync<SqliteException>(() => repository.UpdateAsync(update));

        Assert.Equal(positionsIdsBefore, update.Positionen.Take(2).Select(position => position.Id));
        Assert.Null(update.Positionen[2].Id);
        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        AssertInvoiceEqual(before, persisted);
    }

    [Fact]
    public async Task SetPdfVerknuepfungAsync_WithMatchingVersion_SetsLinkWithoutChangingVersion()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var erwartetGeaendertAm = created.GeaendertAm;
        var verknuepfung = CreatePdfVerknuepfung(erwartetGeaendertAm);

        await repository.SetPdfVerknuepfungAsync(
            created.Id!.Value,
            verknuepfung,
            erwartetGeaendertAm,
            erwartetePdfVerknuepfung: null);

        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        Assert.Equal(erwartetGeaendertAm, persisted.GeaendertAm);
        AssertPdfVerknuepfungEqual(verknuepfung, persisted.PdfVerknuepfung);
    }

    [Fact]
    public async Task SetPdfVerknuepfungAsync_WhenInvoiceIsMissing_ReportsNotFound()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var repository = new RechnungRepository(database.ConnectionString);
        var verknuepfung = CreatePdfVerknuepfung(
            new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SetPdfVerknuepfungAsync(
                9876,
                verknuepfung,
                verknuepfung.RechnungsstandAm,
                erwartetePdfVerknuepfung: null));

        Assert.Contains("9876", exception.Message, StringComparison.Ordinal);
        Assert.Contains("nicht gefunden", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetPdfVerknuepfungAsync_WithStaleVersion_ReportsConcurrencyConflict()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var veralteterStand = created.GeaendertAm;
        var update = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id!.Value));
        update.Titel = "Zwischenzeitlich geänderte Rechnung";
        await repository.UpdateAsync(update);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SetPdfVerknuepfungAsync(
                created.Id.Value,
                CreatePdfVerknuepfung(veralteterStand),
                veralteterStand,
                erwartetePdfVerknuepfung: null));

        Assert.Contains("zwischenzeitlich geändert", exception.Message, StringComparison.Ordinal);
        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        Assert.Null(persisted.PdfVerknuepfung);
        Assert.Equal(update.GeaendertAm, persisted.GeaendertAm);
    }

    [Fact]
    public async Task SetPdfVerknuepfungAsync_WithUnexpectedPreviousLink_RejectsConcurrentReplacement()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var ersteVerknuepfung = CreatePdfVerknuepfung(
            created.GeaendertAm,
            "2026/rechnung-erste.pdf");
        var konkurrierendeVerknuepfung = CreatePdfVerknuepfung(
            created.GeaendertAm,
            "2026/rechnung-konkurrierend.pdf");
        await repository.SetPdfVerknuepfungAsync(
            created.Id!.Value,
            ersteVerknuepfung,
            created.GeaendertAm,
            erwartetePdfVerknuepfung: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SetPdfVerknuepfungAsync(
                created.Id.Value,
                konkurrierendeVerknuepfung,
                created.GeaendertAm,
                erwartetePdfVerknuepfung: null));

        Assert.Contains("PDF-Verknüpfung", exception.Message, StringComparison.Ordinal);
        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        AssertPdfVerknuepfungEqual(ersteVerknuepfung, persisted.PdfVerknuepfung);

        await repository.SetPdfVerknuepfungAsync(
            created.Id.Value,
            konkurrierendeVerknuepfung,
            created.GeaendertAm,
            ersteVerknuepfung);
        persisted = Assert.IsType<Rechnung>(await repository.GetByIdAsync(created.Id.Value));
        AssertPdfVerknuepfungEqual(konkurrierendeVerknuepfung, persisted.PdfVerknuepfung);
    }

    [Fact]
    public async Task UpdateAsync_WithPdfLink_PreservesLinkAndMakesItsInvoiceVersionStale()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var verknuepfung = CreatePdfVerknuepfung(created.GeaendertAm);
        await repository.SetPdfVerknuepfungAsync(
            created.Id!.Value,
            verknuepfung,
            created.GeaendertAm,
            erwartetePdfVerknuepfung: null);
        var update = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));

        update.Titel = "Rechnung nach PDF-Erstellung geändert";
        update.Leistungsdatum = new DateTime(2026, 8, 4);
        update.AbsenderSnapshot!.LogoInhalt = [82, 73, 70, 70];
        update.AbsenderSnapshot.LogoMedientyp = "image/webp";
        await repository.UpdateAsync(update);

        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        Assert.True(persisted.GeaendertAm > verknuepfung.RechnungsstandAm);
        Assert.Equal(new DateTime(2026, 8, 4), persisted.Leistungsdatum);
        Assert.Equal([82, 73, 70, 70], persisted.AbsenderSnapshot!.LogoInhalt);
        Assert.Equal("image/webp", persisted.AbsenderSnapshot.LogoMedientyp);
        AssertPdfVerknuepfungEqual(verknuepfung, persisted.PdfVerknuepfung);
    }

    [Fact]
    public async Task UpdateAsync_AfterConcurrentPdfLink_PreservesNewLinkAndMarksItStale()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var veralteterEditorstand = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id!.Value));
        var verknuepfung = CreatePdfVerknuepfung(veralteterEditorstand.GeaendertAm);

        await repository.SetPdfVerknuepfungAsync(
            created.Id.Value,
            verknuepfung,
            veralteterEditorstand.GeaendertAm,
            erwartetePdfVerknuepfung: null);
        veralteterEditorstand.Titel = "In einem bereits geöffneten Editor geändert";
        await repository.UpdateAsync(veralteterEditorstand);

        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        Assert.Equal(veralteterEditorstand.Titel, persisted.Titel);
        Assert.True(persisted.GeaendertAm > verknuepfung.RechnungsstandAm);
        AssertPdfVerknuepfungEqual(verknuepfung, persisted.PdfVerknuepfung);
    }

    [Fact]
    public async Task GetByIdAsync_WithPartialPdfMetadata_ReportsInconsistentStoredData()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                UPDATE tbl_Rechnung
                SET PdfRelativerPfad = '2026/manipuliert.pdf'
                WHERE Id = @Id;
                """;
            command.Parameters.AddWithValue("@Id", created.Id!.Value);
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.GetByIdAsync(created.Id!.Value));

        Assert.Contains("PDF-Metadaten", exception.Message, StringComparison.Ordinal);
        Assert.Contains("inkonsistent", exception.Message, StringComparison.Ordinal);
        Assert.Contains(created.Id.Value.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteIfUnchangedAsync_WithChangedPdfLink_PreservesInvoice()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));
        var alteVerknuepfung = CreatePdfVerknuepfung(
            created.GeaendertAm,
            "2026/rechnung-alt.pdf");
        var neueVerknuepfung = CreatePdfVerknuepfung(
            created.GeaendertAm,
            "2026/rechnung-neu.pdf");
        await repository.SetPdfVerknuepfungAsync(
            created.Id!.Value,
            alteVerknuepfung,
            created.GeaendertAm,
            erwartetePdfVerknuepfung: null);
        await repository.SetPdfVerknuepfungAsync(
            created.Id.Value,
            neueVerknuepfung,
            created.GeaendertAm,
            alteVerknuepfung);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.DeleteIfUnchangedAsync(
                created.Id.Value,
                created.GeaendertAm,
                alteVerknuepfung));

        Assert.Contains("zwischenzeitlich geändert", exception.Message, StringComparison.Ordinal);
        var persisted = Assert.IsType<Rechnung>(
            await repository.GetByIdAsync(created.Id.Value));
        AssertPdfVerknuepfungEqual(neueVerknuepfung, persisted.PdfVerknuepfung);
    }

    [Fact]
    public async Task DeleteAsync_UsesCascadeForPositions()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));

        await repository.DeleteAsync(created.Id!.Value);

        Assert.Null(await repository.GetByIdAsync(created.Id.Value));
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM tbl_RechnungsPosition
            WHERE RechnungId = @RechnungId;
            """;
        command.Parameters.AddWithValue("@RechnungId", created.Id.Value);
        Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task MissingRecords_AreReported()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var missing = CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3));
        missing.Id = 9876;
        missing.Nummer = "2026-999";

        Assert.Null(await repository.GetByIdAsync(missing.Id.Value));

        var updateException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(missing));
        var deleteException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.DeleteAsync(missing.Id.Value));

        Assert.Contains("9876", updateException.Message, StringComparison.Ordinal);
        Assert.Contains("9876", deleteException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailedCreates_RollBackForeignKeysPositionsAndReservedNumber()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);

        var invalidCustomer = CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3));
        invalidCustomer.KundeId = 999_001;
        invalidCustomer.EmpfaengerSnapshot!.QuellId = invalidCustomer.KundeId.Value;
        await Assert.ThrowsAsync<SqliteException>(() => repository.CreateAsync(invalidCustomer));
        Assert.Null(invalidCustomer.Id);
        Assert.Equal(string.Empty, invalidCustomer.Nummer);

        var invalidProfile = CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 4));
        invalidProfile.FirmaProfilId = 999_002;
        invalidProfile.AbsenderSnapshot!.QuellId = invalidProfile.FirmaProfilId.Value;
        await Assert.ThrowsAsync<SqliteException>(() => repository.CreateAsync(invalidProfile));
        Assert.Null(invalidProfile.Id);
        Assert.Equal(string.Empty, invalidProfile.Nummer);

        var invalidPosition = CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 5));
        invalidPosition.Positionen.Add(new RechnungsPosition
        {
            Beschreibung = "Fehlerhafte Position",
            Menge = 0m,
            Einheit = "ST",
            EinzelpreisNetto = 10m,
            Steuersatz = 19m,
            Reihenfolge = 3
        });
        await Assert.ThrowsAsync<SqliteException>(() => repository.CreateAsync(invalidPosition));
        Assert.Null(invalidPosition.Id);
        Assert.Equal(string.Empty, invalidPosition.Nummer);
        Assert.All(invalidPosition.Positionen, position => Assert.Null(position.Id));

        var valid = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 6)));

        Assert.Equal("2026-001", valid.Nummer);
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM tbl_Rechnung;";
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task CreateAsync_UsesIndependentSequentialNumbersPerYear()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);

        var first2026 = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 1, 10)));
        var second2026 = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 12, 31)));
        var first2027 = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2027, 1, 1)));

        Assert.Equal("2026-001", first2026.Nummer);
        Assert.Equal("2026-002", second2026.Nummer);
        Assert.Equal("2027-001", first2027.Nummer);
    }

    [Fact]
    public async Task CreateAsync_WhenSequenceExceeds999_DoesNotTruncateNumber()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO tbl_RechnungsnummerSequenz (Jahr, LetzteNummer)
                VALUES (2026, 999);
                """;
            await command.ExecuteNonQueryAsync();
        }

        var created = await repository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));

        Assert.Equal("2026-1000", created.Nummer);
    }

    [Fact]
    public async Task ConcurrentCreates_ReceiveDifferentNumbers()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var first = CreateInvoice(kunde, firmaProfil, new DateTime(2028, 3, 1));
        var second = CreateInvoice(kunde, firmaProfil, new DateTime(2028, 3, 2));

        var created = await Task.WhenAll(
            repository.CreateAsync(first),
            repository.CreateAsync(second));

        Assert.Equal(
            new[] { "2028-001", "2028-002" },
            created.Select(rechnung => rechnung.Nummer).OrderBy(nummer => nummer));
        Assert.Equal(2, created.Select(rechnung => rechnung.Id).Distinct().Count());
    }

    [Fact]
    public async Task GetAllAsync_FiltersCanonicalStatusAndSortsByDateThenIdDescending()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var repository = new RechnungRepository(database.ConnectionString);
        var oldPaid = CreateInvoice(
            kunde,
            firmaProfil,
            new DateTime(2026, 7, 1),
            RechnungsStatus.Bezahlt,
            "Alte bezahlte Rechnung");
        var newOpen = CreateInvoice(
            kunde,
            firmaProfil,
            new DateTime(2026, 8, 1),
            RechnungsStatus.Offen,
            "Neue offene Rechnung");
        var newPaid = CreateInvoice(
            kunde,
            firmaProfil,
            new DateTime(2026, 8, 1),
            RechnungsStatus.Bezahlt,
            "Neue bezahlte Rechnung");
        await repository.CreateAsync(oldPaid);
        await repository.CreateAsync(newOpen);
        await repository.CreateAsync(newPaid);
        var pdfVerknuepfung = CreatePdfVerknuepfung(
            newPaid.GeaendertAm,
            "2026/rechnung-3-2026-002.pdf");
        await repository.SetPdfVerknuepfungAsync(
            newPaid.Id!.Value,
            pdfVerknuepfung,
            newPaid.GeaendertAm,
            erwartetePdfVerknuepfung: null);

        var all = await repository.GetAllAsync();
        var paid = await repository.GetAllAsync(RechnungsStatus.Bezahlt);

        Assert.Equal(
            new[] { newPaid.Id, newOpen.Id, oldPaid.Id },
            all.Select(rechnung => (int?)rechnung.Id));
        Assert.Equal(
            new[] { newPaid.Id, oldPaid.Id },
            paid.Select(rechnung => (int?)rechnung.Id));
        Assert.All(paid, rechnung => Assert.Equal(RechnungsStatus.Bezahlt, rechnung.Status));
        Assert.All(all, rechnung => Assert.Equal("Synthetischer Kunde GmbH", rechnung.KundeName));
        Assert.All(all, rechnung => Assert.NotEqual(default, rechnung.GeaendertAm));
        var newPaidOverview = Assert.Single(all, rechnung => rechnung.Id == newPaid.Id);
        Assert.Equal(newPaid.GeaendertAm, newPaidOverview.GeaendertAm);
        Assert.Equal(pdfVerknuepfung.RelativerPfad, newPaidOverview.PdfRelativerPfad);
        Assert.Equal(pdfVerknuepfung.ErstelltAm, newPaidOverview.PdfErstelltAm);
        Assert.Equal(pdfVerknuepfung.RechnungsstandAm, newPaidOverview.PdfRechnungsstandAm);
        Assert.All(
            all.Where(rechnung => rechnung.Id != newPaid.Id),
            rechnung =>
            {
                Assert.Null(rechnung.PdfRelativerPfad);
                Assert.Null(rechnung.PdfErstelltAm);
                Assert.Null(rechnung.PdfRechnungsstandAm);
            });
        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetAllAsync("Bezahlt"));
    }

    [Fact]
    public async Task GetByIdAsync_AfterMasterDataChanges_PreservesSnapshots()
    {
        using var database = await CreateMigratedDatabaseAsync();
        var (kunde, firmaProfil) = await CreateMasterDataAsync(database);
        var rechnungRepository = new RechnungRepository(database.ConnectionString);
        var kundenRepository = new KundeRepository(database.ConnectionString);
        var firmaRepository = new FirmaProfilRepository(database.ConnectionString);
        var created = await rechnungRepository.CreateAsync(
            CreateInvoice(kunde, firmaProfil, new DateTime(2026, 8, 3)));

        kunde.Firmenname = "Geänderter Kundenname AG";
        kunde.Ansprechpartner = "Neue Kontaktperson";
        kunde.Strasse = "Neue Kundenstraße 99";
        kunde.Land = "AT";
        await kundenRepository.UpdateAsync(kunde);

        firmaProfil.Name = "Geänderter Absender AG";
        firmaProfil.Ansprechpartner = "Neue Absenderperson";
        firmaProfil.Strasse = "Neue Absenderstraße 88";
        firmaProfil.Land = "CH";
        await firmaRepository.UpdateAsync(firmaProfil);

        var persisted = Assert.IsType<Rechnung>(
            await rechnungRepository.GetByIdAsync(created.Id!.Value));
        var overview = Assert.Single(await rechnungRepository.GetAllAsync());

        Assert.Equal("Synthetischer Kunde GmbH", persisted.EmpfaengerSnapshot!.Name);
        Assert.Equal("Karla Kunde", persisted.EmpfaengerSnapshot.Ansprechpartner);
        Assert.Equal("Kundenweg 7", persisted.EmpfaengerSnapshot.Strasse);
        Assert.Equal("DE", persisted.EmpfaengerSnapshot.Land);
        Assert.Equal("Synthetische Absender GmbH", persisted.AbsenderSnapshot!.Name);
        Assert.Equal("Fiona Firma", persisted.AbsenderSnapshot.Ansprechpartner);
        Assert.Equal("Firmenstraße 12", persisted.AbsenderSnapshot.Strasse);
        Assert.Equal("DE", persisted.AbsenderSnapshot.Land);
        Assert.Equal("Synthetischer Kunde GmbH", overview.KundeName);
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

    private static async Task<(Kunde Kunde, FirmaProfil FirmaProfil)> CreateMasterDataAsync(
        TemporarySqliteDatabase database)
    {
        var kundenRepository = new KundeRepository(database.ConnectionString);
        var firmaRepository = new FirmaProfilRepository(database.ConnectionString);
        var kunde = await kundenRepository.CreateAsync(new Kunde
        {
            Firmenname = "Synthetischer Kunde GmbH",
            Ansprechpartner = "Karla Kunde",
            Strasse = "Kundenweg 7",
            PLZ = "23456",
            Ort = "Kundenstadt",
            Land = "DE",
            Email = "karla@kunde.example",
            Telefon = "+49 40 555000",
            UstIdNr = "DE987654321",
            Bemerkung = "Synthetische Stammdaten",
            ErstelltAm = new DateTime(2026, 1, 2, 9, 30, 0, DateTimeKind.Utc)
        });
        var firmaProfil = await firmaRepository.CreateAsync(new FirmaProfil
        {
            Name = "Synthetische Absender GmbH",
            LogoPfad = "logos/synthetisch.svg",
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
        });
        return (kunde, firmaProfil);
    }

    private static Rechnung CreateInvoice(
        Kunde kunde,
        FirmaProfil firmaProfil,
        DateTime rechnungsdatum,
        string status = RechnungsStatus.Erstellt,
        string titel = "Synthetische Integrationsrechnung")
    {
        var rechnung = new Rechnung
        {
            Titel = titel,
            Erstellungsdatum = rechnungsdatum.AddDays(-1),
            Rechnungsdatum = rechnungsdatum,
            Faeligkeitsdatum = rechnungsdatum.AddDays(14),
            KundeId = kunde.Id,
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = kunde.Id!.Value,
                Name = kunde.Firmenname,
                Ansprechpartner = kunde.Ansprechpartner,
                Strasse = kunde.Strasse,
                PLZ = kunde.PLZ,
                Ort = kunde.Ort,
                Land = kunde.Land,
                Email = kunde.Email,
                UstIdNr = kunde.UstIdNr
            },
            FirmaProfilId = firmaProfil.Id,
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = firmaProfil.Id!.Value,
                Name = firmaProfil.Name,
                LogoPfad = firmaProfil.LogoPfad,
                Ansprechpartner = firmaProfil.Ansprechpartner,
                Strasse = firmaProfil.Strasse,
                PLZ = firmaProfil.PLZ,
                Ort = firmaProfil.Ort,
                Land = firmaProfil.Land,
                Email = firmaProfil.Email,
                Telefon = firmaProfil.Telefon,
                UstIdNr = firmaProfil.UstIdNr,
                IBAN = firmaProfil.IBAN,
                BIC = firmaProfil.BIC
            },
            Waehrung = "EUR",
            Status = status,
            Bemerkung = "Ausschließlich synthetische Testdaten",
            ErstelltAm = new DateTime(2026, 8, 3, 10, 15, 0, DateTimeKind.Utc),
            GeaendertAm = new DateTime(2026, 8, 3, 10, 15, 0, DateTimeKind.Utc),
            Positionen =
            [
                new RechnungsPosition
                {
                    Beschreibung = "Synthetische Beratungsleistung",
                    Menge = 1.25m,
                    Einheit = "STD",
                    EinzelpreisNetto = 123.456m,
                    Steuersatz = 19m,
                    Reihenfolge = 1
                },
                new RechnungsPosition
                {
                    Beschreibung = "Synthetische Materialposition",
                    Menge = 2.5m,
                    Einheit = "ST",
                    EinzelpreisNetto = 10.99m,
                    Steuersatz = 7m,
                    Reihenfolge = 2
                }
            ]
        };
        return RechnungCalculator.Berechnen(rechnung);
    }

    private static void AssertInvoiceEqual(Rechnung expected, Rechnung actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Nummer, actual.Nummer);
        Assert.Equal(expected.Titel, actual.Titel);
        Assert.Equal(expected.Erstellungsdatum, actual.Erstellungsdatum);
        Assert.Equal(expected.Rechnungsdatum, actual.Rechnungsdatum);
        Assert.Equal(expected.Leistungsdatum, actual.Leistungsdatum);
        Assert.Equal(expected.Faeligkeitsdatum, actual.Faeligkeitsdatum);
        Assert.Equal(expected.KundeId, actual.KundeId);
        Assert.Equal(expected.FirmaProfilId, actual.FirmaProfilId);
        Assert.Equal(expected.GesamtbetragNetto, actual.GesamtbetragNetto);
        Assert.Equal(expected.UmsatzsteuerBetrag, actual.UmsatzsteuerBetrag);
        Assert.Equal(expected.GesamtsteuerRate, actual.GesamtsteuerRate);
        Assert.Equal(expected.GesamtbetragBrutto, actual.GesamtbetragBrutto);
        Assert.Equal(expected.Waehrung, actual.Waehrung);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.Bemerkung, actual.Bemerkung);
        Assert.Equal(expected.ErstelltAm, actual.ErstelltAm);
        Assert.Equal(expected.GeaendertAm, actual.GeaendertAm);
        AssertPdfVerknuepfungEqual(expected.PdfVerknuepfung, actual.PdfVerknuepfung);
        AssertEmpfaengerSnapshotEqual(expected.EmpfaengerSnapshot!, actual.EmpfaengerSnapshot!);
        AssertAbsenderSnapshotEqual(expected.AbsenderSnapshot!, actual.AbsenderSnapshot!);
        Assert.Equal(expected.Positionen.Count, actual.Positionen.Count);
        for (var index = 0; index < expected.Positionen.Count; index++)
        {
            AssertPositionEqual(expected.Positionen[index], actual.Positionen[index]);
        }
    }

    private static void AssertEmpfaengerSnapshotEqual(
        RechnungsEmpfaengerSnapshot expected,
        RechnungsEmpfaengerSnapshot actual)
    {
        Assert.Equal(expected.QuellId, actual.QuellId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Ansprechpartner, actual.Ansprechpartner);
        Assert.Equal(expected.Strasse, actual.Strasse);
        Assert.Equal(expected.PLZ, actual.PLZ);
        Assert.Equal(expected.Ort, actual.Ort);
        Assert.Equal(expected.Land, actual.Land);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.UstIdNr, actual.UstIdNr);
    }

    private static void AssertAbsenderSnapshotEqual(
        RechnungsAbsenderSnapshot expected,
        RechnungsAbsenderSnapshot actual)
    {
        Assert.Equal(expected.QuellId, actual.QuellId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.LogoPfad, actual.LogoPfad);
        Assert.Equal(expected.LogoInhalt, actual.LogoInhalt);
        Assert.Equal(expected.LogoMedientyp, actual.LogoMedientyp);
        Assert.Equal(expected.Ansprechpartner, actual.Ansprechpartner);
        Assert.Equal(expected.Strasse, actual.Strasse);
        Assert.Equal(expected.PLZ, actual.PLZ);
        Assert.Equal(expected.Ort, actual.Ort);
        Assert.Equal(expected.Land, actual.Land);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Telefon, actual.Telefon);
        Assert.Equal(expected.UstIdNr, actual.UstIdNr);
        Assert.Equal(expected.IBAN, actual.IBAN);
        Assert.Equal(expected.BIC, actual.BIC);
    }

    private static RechnungsPdfVerknuepfung CreatePdfVerknuepfung(
        DateTime rechnungsstandAm,
        string relativerPfad = "2026/rechnung-1-2026-001.pdf")
    {
        return new RechnungsPdfVerknuepfung(
            relativerPfad,
            new DateTime(2026, 8, 3, 12, 30, 0, DateTimeKind.Utc),
            rechnungsstandAm);
    }

    private static void AssertPdfVerknuepfungEqual(
        RechnungsPdfVerknuepfung? expected,
        RechnungsPdfVerknuepfung? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        var actualValue = Assert.IsType<RechnungsPdfVerknuepfung>(actual);
        Assert.Equal(expected.RelativerPfad, actualValue.RelativerPfad);
        Assert.Equal(expected.ErstelltAm, actualValue.ErstelltAm);
        Assert.Equal(expected.RechnungsstandAm, actualValue.RechnungsstandAm);
    }

    private static void AssertPositionEqual(RechnungsPosition expected, RechnungsPosition actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.RechnungId, actual.RechnungId);
        Assert.Equal(expected.Reihenfolge, actual.Reihenfolge);
        Assert.Equal(expected.Beschreibung, actual.Beschreibung);
        Assert.Equal(expected.Menge, actual.Menge);
        Assert.Equal(expected.Einheit, actual.Einheit);
        Assert.Equal(expected.EinzelpreisNetto, actual.EinzelpreisNetto);
        Assert.Equal(expected.Steuersatz, actual.Steuersatz);
        Assert.Equal(expected.GesamtpreisNetto, actual.GesamtpreisNetto);
    }
}
