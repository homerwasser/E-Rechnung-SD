using System.Globalization;
using Dapper;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using Microsoft.Data.Sqlite;

namespace ERechnung.Data.Repositories;

public sealed class RechnungRepository : IRechnungRepository
{
    private readonly string _connectionString;

    public RechnungRepository(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException(
                "Die Verbindungszeichenfolge darf nicht leer sein.",
                nameof(connectionString))
            : connectionString;
    }

    public async Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null)
    {
        if (status is not null && !RechnungsStatus.IsValid(status))
        {
            throw new ArgumentException("Der Rechnungsstatus ist ungültig.", nameof(status));
        }

        const string selectSql = """
            SELECT Id,
                   Nummer,
                   Rechnungsdatum,
                   EmpfaengerSnapshotName AS KundeName,
                   GesamtbetragBrutto,
                   Status
            FROM tbl_Rechnung
            """;
        var sql = status is null
            ? selectSql + "\nORDER BY Rechnungsdatum DESC, Id DESC;"
            : selectSql + "\nWHERE Status = @Status ORDER BY Rechnungsdatum DESC, Id DESC;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var rechnungen = await connection.QueryAsync<RechnungsUebersicht>(
            sql,
            new { Status = status });
        return rechnungen.AsList();
    }

    public async Task<Rechnung?> GetByIdAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var datensatz = await connection.QuerySingleOrDefaultAsync<RechnungDatensatz>("""
                SELECT Id,
                       Nummer,
                       Titel,
                       Erstellungsdatum,
                       Rechnungsdatum,
                       Faelligkeitsdatum AS Faeligkeitsdatum,
                       KundeId,
                       FirmaProfilId,
                       GesamtbetragNetto,
                       UmsatzsteuerBetrag,
                       GesamtsteuerRate,
                       GesamtbetragBrutto,
                       Waehrung,
                       Status,
                       Bemerkung,
                       ErstelltAm,
                       GeaendertAm,
                       EmpfaengerSnapshotName,
                       EmpfaengerSnapshotAnsprechpartner,
                       EmpfaengerSnapshotStrasse,
                       EmpfaengerSnapshotPLZ,
                       EmpfaengerSnapshotOrt,
                       EmpfaengerSnapshotLand,
                       EmpfaengerSnapshotEmail,
                       EmpfaengerSnapshotUstIdNr,
                       AbsenderSnapshotName,
                       AbsenderSnapshotLogoPfad,
                       AbsenderSnapshotAnsprechpartner,
                       AbsenderSnapshotStrasse,
                       AbsenderSnapshotPLZ,
                       AbsenderSnapshotOrt,
                       AbsenderSnapshotLand,
                       AbsenderSnapshotEmail,
                       AbsenderSnapshotTelefon,
                       AbsenderSnapshotUstIdNr,
                       AbsenderSnapshotIBAN,
                       AbsenderSnapshotBIC
                FROM tbl_Rechnung
                WHERE Id = @Id;
                """, new { Id = id }, transaction);

            if (datensatz is null)
            {
                await transaction.CommitAsync();
                return null;
            }

            var positionen = await connection.QueryAsync<RechnungsPosition>("""
                SELECT Id,
                       RechnungId,
                       Reihenfolge,
                       Beschreibung,
                       Menge,
                       Einheit,
                       EinzelpreisNetto,
                       Steuersatz
                FROM tbl_RechnungsPosition
                WHERE RechnungId = @Id
                ORDER BY Reihenfolge, Id;
                """, new { Id = id }, transaction);

            var rechnung = datensatz.ToModel(positionen.AsList());
            await transaction.CommitAsync();
            return rechnung;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Rechnung> CreateAsync(Rechnung rechnung)
    {
        ArgumentNullException.ThrowIfNull(rechnung);
        if (rechnung.Id is not null)
        {
            throw new ArgumentException(
                "Eine neue Rechnung darf noch keine ID besitzen.",
                nameof(rechnung));
        }

        var positionen = GetPositionen(rechnung);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var zeitstempel = DateTime.UtcNow;
            var naechsteNummer = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tbl_RechnungsnummerSequenz (Jahr, LetzteNummer)
                VALUES (@Jahr, 1)
                ON CONFLICT(Jahr) DO UPDATE
                    SET LetzteNummer = LetzteNummer + 1
                RETURNING LetzteNummer;
                """, new { Jahr = rechnung.Rechnungsdatum.Year }, transaction);
            var nummer = FormatiereRechnungsnummer(rechnung.Rechnungsdatum.Year, naechsteNummer);

            var rechnungId = checked((int)await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tbl_Rechnung (
                    Nummer,
                    Titel,
                    Erstellungsdatum,
                    Rechnungsdatum,
                    Faelligkeitsdatum,
                    KundeId,
                    FirmaProfilId,
                    GesamtbetragNetto,
                    UmsatzsteuerBetrag,
                    GesamtsteuerRate,
                    GesamtbetragBrutto,
                    Waehrung,
                    Status,
                    Bemerkung,
                    ErstelltAm,
                    GeaendertAm,
                    EmpfaengerSnapshotName,
                    EmpfaengerSnapshotAnsprechpartner,
                    EmpfaengerSnapshotStrasse,
                    EmpfaengerSnapshotPLZ,
                    EmpfaengerSnapshotOrt,
                    EmpfaengerSnapshotLand,
                    EmpfaengerSnapshotEmail,
                    EmpfaengerSnapshotUstIdNr,
                    AbsenderSnapshotName,
                    AbsenderSnapshotLogoPfad,
                    AbsenderSnapshotAnsprechpartner,
                    AbsenderSnapshotStrasse,
                    AbsenderSnapshotPLZ,
                    AbsenderSnapshotOrt,
                    AbsenderSnapshotLand,
                    AbsenderSnapshotEmail,
                    AbsenderSnapshotTelefon,
                    AbsenderSnapshotUstIdNr,
                    AbsenderSnapshotIBAN,
                    AbsenderSnapshotBIC
                )
                VALUES (
                    @Nummer,
                    @Titel,
                    @Erstellungsdatum,
                    @Rechnungsdatum,
                    @Faelligkeitsdatum,
                    @KundeId,
                    @FirmaProfilId,
                    @GesamtbetragNetto,
                    @UmsatzsteuerBetrag,
                    @GesamtsteuerRate,
                    @GesamtbetragBrutto,
                    @Waehrung,
                    @Status,
                    @Bemerkung,
                    @ErstelltAm,
                    @GeaendertAm,
                    @EmpfaengerSnapshotName,
                    @EmpfaengerSnapshotAnsprechpartner,
                    @EmpfaengerSnapshotStrasse,
                    @EmpfaengerSnapshotPLZ,
                    @EmpfaengerSnapshotOrt,
                    @EmpfaengerSnapshotLand,
                    @EmpfaengerSnapshotEmail,
                    @EmpfaengerSnapshotUstIdNr,
                    @AbsenderSnapshotName,
                    @AbsenderSnapshotLogoPfad,
                    @AbsenderSnapshotAnsprechpartner,
                    @AbsenderSnapshotStrasse,
                    @AbsenderSnapshotPLZ,
                    @AbsenderSnapshotOrt,
                    @AbsenderSnapshotLand,
                    @AbsenderSnapshotEmail,
                    @AbsenderSnapshotTelefon,
                    @AbsenderSnapshotUstIdNr,
                    @AbsenderSnapshotIBAN,
                    @AbsenderSnapshotBIC
                );
                SELECT last_insert_rowid();
                """, CreateParameters(rechnung, nummer, zeitstempel, zeitstempel), transaction));

            var positionsIds = await InsertPositionenAsync(
                connection,
                transaction,
                rechnungId,
                positionen);

            await transaction.CommitAsync();

            rechnung.Id = rechnungId;
            rechnung.Nummer = nummer;
            rechnung.ErstelltAm = zeitstempel;
            rechnung.GeaendertAm = zeitstempel;
            AktualisierePositionsIds(positionen, positionsIds, rechnungId);
            return rechnung;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(Rechnung rechnung)
    {
        ArgumentNullException.ThrowIfNull(rechnung);
        if (rechnung.Id is null)
        {
            throw new ArgumentException(
                "Eine Rechnung ohne ID kann nicht aktualisiert werden.",
                nameof(rechnung));
        }

        var rechnungId = rechnung.Id.Value;
        var positionen = GetPositionen(rechnung);
        var bisherGeaendertAm = rechnung.GeaendertAm;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var gespeicherteNummer = await connection.QuerySingleOrDefaultAsync<string>("""
                SELECT Nummer
                FROM tbl_Rechnung
                WHERE Id = @Id;
                """, new { Id = rechnungId }, transaction);

            if (gespeicherteNummer is null)
            {
                throw new InvalidOperationException(
                    $"Die Rechnung mit der ID {rechnungId} wurde nicht gefunden.");
            }

            var geaendertAm = ErstelleNeuenAenderungszeitpunkt(bisherGeaendertAm);
            var parameters = CreateParameters(
                rechnung,
                gespeicherteNummer,
                rechnung.ErstelltAm,
                geaendertAm);
            parameters.Add("BisherGeaendertAm", bisherGeaendertAm);
            var affectedRows = await connection.ExecuteAsync("""
                UPDATE tbl_Rechnung
                SET Titel = @Titel,
                    Erstellungsdatum = @Erstellungsdatum,
                    Rechnungsdatum = @Rechnungsdatum,
                    Faelligkeitsdatum = @Faelligkeitsdatum,
                    KundeId = @KundeId,
                    FirmaProfilId = @FirmaProfilId,
                    GesamtbetragNetto = @GesamtbetragNetto,
                    UmsatzsteuerBetrag = @UmsatzsteuerBetrag,
                    GesamtsteuerRate = @GesamtsteuerRate,
                    GesamtbetragBrutto = @GesamtbetragBrutto,
                    Waehrung = @Waehrung,
                    Status = @Status,
                    Bemerkung = @Bemerkung,
                    GeaendertAm = @GeaendertAm,
                    EmpfaengerSnapshotName = @EmpfaengerSnapshotName,
                    EmpfaengerSnapshotAnsprechpartner = @EmpfaengerSnapshotAnsprechpartner,
                    EmpfaengerSnapshotStrasse = @EmpfaengerSnapshotStrasse,
                    EmpfaengerSnapshotPLZ = @EmpfaengerSnapshotPLZ,
                    EmpfaengerSnapshotOrt = @EmpfaengerSnapshotOrt,
                    EmpfaengerSnapshotLand = @EmpfaengerSnapshotLand,
                    EmpfaengerSnapshotEmail = @EmpfaengerSnapshotEmail,
                    EmpfaengerSnapshotUstIdNr = @EmpfaengerSnapshotUstIdNr,
                    AbsenderSnapshotName = @AbsenderSnapshotName,
                    AbsenderSnapshotLogoPfad = @AbsenderSnapshotLogoPfad,
                    AbsenderSnapshotAnsprechpartner = @AbsenderSnapshotAnsprechpartner,
                    AbsenderSnapshotStrasse = @AbsenderSnapshotStrasse,
                    AbsenderSnapshotPLZ = @AbsenderSnapshotPLZ,
                    AbsenderSnapshotOrt = @AbsenderSnapshotOrt,
                    AbsenderSnapshotLand = @AbsenderSnapshotLand,
                    AbsenderSnapshotEmail = @AbsenderSnapshotEmail,
                    AbsenderSnapshotTelefon = @AbsenderSnapshotTelefon,
                    AbsenderSnapshotUstIdNr = @AbsenderSnapshotUstIdNr,
                    AbsenderSnapshotIBAN = @AbsenderSnapshotIBAN,
                    AbsenderSnapshotBIC = @AbsenderSnapshotBIC
                WHERE Id = @Id
                  AND GeaendertAm = @BisherGeaendertAm;
                """, parameters, transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Die Rechnung mit der ID {rechnungId} wurde zwischenzeitlich geändert. "
                    + "Laden Sie die Rechnung neu und wiederholen Sie den Vorgang.");
            }

            await connection.ExecuteAsync(
                "DELETE FROM tbl_RechnungsPosition WHERE RechnungId = @RechnungId;",
                new { RechnungId = rechnungId },
                transaction);

            var positionsIds = await InsertPositionenAsync(
                connection,
                transaction,
                rechnungId,
                positionen);

            await transaction.CommitAsync();

            rechnung.Nummer = gespeicherteNummer;
            rechnung.GeaendertAm = geaendertAm;
            AktualisierePositionsIds(positionen, positionsIds, rechnungId);
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
                "DELETE FROM tbl_Rechnung WHERE Id = @Id;",
                new { Id = id },
                transaction);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Die Rechnung mit der ID {id} wurde nicht gefunden.");
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static List<RechnungsPosition> GetPositionen(Rechnung rechnung)
    {
        return rechnung.Positionen?.ToList()
            ?? throw new ArgumentException(
                "Die Positionsliste der Rechnung darf nicht null sein.",
                nameof(rechnung));
    }

    private static DynamicParameters CreateParameters(
        Rechnung rechnung,
        string nummer,
        DateTime erstelltAm,
        DateTime geaendertAm)
    {
        var empfaenger = rechnung.EmpfaengerSnapshot
            ?? throw new ArgumentException(
                "Der Rechnungsempfänger-Snapshot ist erforderlich.",
                nameof(rechnung));
        var absender = rechnung.AbsenderSnapshot
            ?? throw new ArgumentException(
                "Der Rechnungsabsender-Snapshot ist erforderlich.",
                nameof(rechnung));

        var parameters = new DynamicParameters();
        parameters.Add("Id", rechnung.Id);
        parameters.Add("Nummer", nummer);
        parameters.Add("Titel", rechnung.Titel);
        parameters.Add("Erstellungsdatum", rechnung.Erstellungsdatum);
        parameters.Add("Rechnungsdatum", rechnung.Rechnungsdatum);
        parameters.Add("Faelligkeitsdatum", rechnung.Faeligkeitsdatum);
        parameters.Add("KundeId", rechnung.KundeId);
        parameters.Add("FirmaProfilId", rechnung.FirmaProfilId);
        parameters.Add("GesamtbetragNetto", rechnung.GesamtbetragNetto);
        parameters.Add("UmsatzsteuerBetrag", rechnung.UmsatzsteuerBetrag);
        parameters.Add("GesamtsteuerRate", rechnung.GesamtsteuerRate);
        parameters.Add("GesamtbetragBrutto", rechnung.GesamtbetragBrutto);
        parameters.Add("Waehrung", rechnung.Waehrung);
        parameters.Add("Status", rechnung.Status);
        parameters.Add("Bemerkung", rechnung.Bemerkung);
        parameters.Add("ErstelltAm", erstelltAm);
        parameters.Add("GeaendertAm", geaendertAm);
        parameters.Add("EmpfaengerSnapshotName", empfaenger.Name);
        parameters.Add("EmpfaengerSnapshotAnsprechpartner", empfaenger.Ansprechpartner);
        parameters.Add("EmpfaengerSnapshotStrasse", empfaenger.Strasse);
        parameters.Add("EmpfaengerSnapshotPLZ", empfaenger.PLZ);
        parameters.Add("EmpfaengerSnapshotOrt", empfaenger.Ort);
        parameters.Add("EmpfaengerSnapshotLand", empfaenger.Land);
        parameters.Add("EmpfaengerSnapshotEmail", empfaenger.Email);
        parameters.Add("EmpfaengerSnapshotUstIdNr", empfaenger.UstIdNr);
        parameters.Add("AbsenderSnapshotName", absender.Name);
        parameters.Add("AbsenderSnapshotLogoPfad", absender.LogoPfad);
        parameters.Add("AbsenderSnapshotAnsprechpartner", absender.Ansprechpartner);
        parameters.Add("AbsenderSnapshotStrasse", absender.Strasse);
        parameters.Add("AbsenderSnapshotPLZ", absender.PLZ);
        parameters.Add("AbsenderSnapshotOrt", absender.Ort);
        parameters.Add("AbsenderSnapshotLand", absender.Land);
        parameters.Add("AbsenderSnapshotEmail", absender.Email);
        parameters.Add("AbsenderSnapshotTelefon", absender.Telefon);
        parameters.Add("AbsenderSnapshotUstIdNr", absender.UstIdNr);
        parameters.Add("AbsenderSnapshotIBAN", absender.IBAN);
        parameters.Add("AbsenderSnapshotBIC", absender.BIC);
        return parameters;
    }

    private static async Task<List<int>> InsertPositionenAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int rechnungId,
        IReadOnlyList<RechnungsPosition> positionen)
    {
        var ids = new List<int>(positionen.Count);
        foreach (var position in positionen)
        {
            if (position is null)
            {
                throw new ArgumentException("Eine Rechnungsposition darf nicht null sein.");
            }

            var id = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tbl_RechnungsPosition (
                    RechnungId,
                    Beschreibung,
                    Menge,
                    Einheit,
                    EinzelpreisNetto,
                    Steuersatz,
                    Reihenfolge
                )
                VALUES (
                    @RechnungId,
                    @Beschreibung,
                    @Menge,
                    @Einheit,
                    @EinzelpreisNetto,
                    @Steuersatz,
                    @Reihenfolge
                );
                SELECT last_insert_rowid();
                """, new
            {
                RechnungId = rechnungId,
                position.Beschreibung,
                position.Menge,
                position.Einheit,
                position.EinzelpreisNetto,
                position.Steuersatz,
                position.Reihenfolge
            }, transaction);
            ids.Add(checked((int)id));
        }

        return ids;
    }

    private static void AktualisierePositionsIds(
        IReadOnlyList<RechnungsPosition> positionen,
        IReadOnlyList<int> ids,
        int rechnungId)
    {
        for (var index = 0; index < positionen.Count; index++)
        {
            positionen[index].Id = ids[index];
            positionen[index].RechnungId = rechnungId;
        }
    }

    private static DateTime ErstelleNeuenAenderungszeitpunkt(DateTime bisherGeaendertAm)
    {
        var jetzt = DateTime.UtcNow;
        if (jetzt > bisherGeaendertAm)
        {
            return jetzt;
        }

        return DateTime.SpecifyKind(bisherGeaendertAm.AddTicks(1), DateTimeKind.Utc);
    }

    private static string FormatiereRechnungsnummer(int jahr, long laufendeNummer)
    {
        return string.Concat(
            jahr.ToString("D4", CultureInfo.InvariantCulture),
            "-",
            laufendeNummer.ToString("D3", CultureInfo.InvariantCulture));
    }

    private sealed class RechnungDatensatz
    {
        public int Id { get; set; }
        public string Nummer { get; set; } = string.Empty;
        public string Titel { get; set; } = string.Empty;
        public DateTime Erstellungsdatum { get; set; }
        public DateTime Rechnungsdatum { get; set; }
        public DateTime? Faeligkeitsdatum { get; set; }
        public int KundeId { get; set; }
        public int FirmaProfilId { get; set; }
        public decimal GesamtbetragNetto { get; set; }
        public decimal UmsatzsteuerBetrag { get; set; }
        public decimal GesamtsteuerRate { get; set; }
        public decimal GesamtbetragBrutto { get; set; }
        public string Waehrung { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Bemerkung { get; set; } = string.Empty;
        public DateTime ErstelltAm { get; set; }
        public DateTime GeaendertAm { get; set; }
        public string EmpfaengerSnapshotName { get; set; } = string.Empty;
        public string EmpfaengerSnapshotAnsprechpartner { get; set; } = string.Empty;
        public string EmpfaengerSnapshotStrasse { get; set; } = string.Empty;
        public string EmpfaengerSnapshotPLZ { get; set; } = string.Empty;
        public string EmpfaengerSnapshotOrt { get; set; } = string.Empty;
        public string EmpfaengerSnapshotLand { get; set; } = string.Empty;
        public string EmpfaengerSnapshotEmail { get; set; } = string.Empty;
        public string? EmpfaengerSnapshotUstIdNr { get; set; }
        public string AbsenderSnapshotName { get; set; } = string.Empty;
        public string AbsenderSnapshotLogoPfad { get; set; } = string.Empty;
        public string AbsenderSnapshotAnsprechpartner { get; set; } = string.Empty;
        public string AbsenderSnapshotStrasse { get; set; } = string.Empty;
        public string AbsenderSnapshotPLZ { get; set; } = string.Empty;
        public string AbsenderSnapshotOrt { get; set; } = string.Empty;
        public string AbsenderSnapshotLand { get; set; } = string.Empty;
        public string AbsenderSnapshotEmail { get; set; } = string.Empty;
        public string AbsenderSnapshotTelefon { get; set; } = string.Empty;
        public string? AbsenderSnapshotUstIdNr { get; set; }
        public string AbsenderSnapshotIBAN { get; set; } = string.Empty;
        public string AbsenderSnapshotBIC { get; set; } = string.Empty;

        public Rechnung ToModel(List<RechnungsPosition> positionen)
        {
            return new Rechnung
            {
                Id = Id,
                Nummer = Nummer,
                Titel = Titel,
                Erstellungsdatum = Erstellungsdatum,
                Rechnungsdatum = Rechnungsdatum,
                Faeligkeitsdatum = Faeligkeitsdatum,
                KundeId = KundeId,
                EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
                {
                    QuellId = KundeId,
                    Name = EmpfaengerSnapshotName,
                    Ansprechpartner = EmpfaengerSnapshotAnsprechpartner,
                    Strasse = EmpfaengerSnapshotStrasse,
                    PLZ = EmpfaengerSnapshotPLZ,
                    Ort = EmpfaengerSnapshotOrt,
                    Land = EmpfaengerSnapshotLand,
                    Email = EmpfaengerSnapshotEmail,
                    UstIdNr = EmpfaengerSnapshotUstIdNr
                },
                FirmaProfilId = FirmaProfilId,
                AbsenderSnapshot = new RechnungsAbsenderSnapshot
                {
                    QuellId = FirmaProfilId,
                    Name = AbsenderSnapshotName,
                    LogoPfad = AbsenderSnapshotLogoPfad,
                    Ansprechpartner = AbsenderSnapshotAnsprechpartner,
                    Strasse = AbsenderSnapshotStrasse,
                    PLZ = AbsenderSnapshotPLZ,
                    Ort = AbsenderSnapshotOrt,
                    Land = AbsenderSnapshotLand,
                    Email = AbsenderSnapshotEmail,
                    Telefon = AbsenderSnapshotTelefon,
                    UstIdNr = AbsenderSnapshotUstIdNr,
                    IBAN = AbsenderSnapshotIBAN,
                    BIC = AbsenderSnapshotBIC
                },
                GesamtbetragNetto = GesamtbetragNetto,
                UmsatzsteuerBetrag = UmsatzsteuerBetrag,
                GesamtsteuerRate = GesamtsteuerRate,
                GesamtbetragBrutto = GesamtbetragBrutto,
                Waehrung = Waehrung,
                Positionen = positionen,
                Status = Status,
                Bemerkung = Bemerkung,
                ErstelltAm = ErstelltAm,
                GeaendertAm = GeaendertAm
            };
        }
    }
}
