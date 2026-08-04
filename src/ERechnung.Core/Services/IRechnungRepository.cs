using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public interface IRechnungRepository
{
    Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null);
    Task<Rechnung?> GetByIdAsync(int id);
    Task<Rechnung> CreateAsync(Rechnung rechnung);
    Task UpdateAsync(Rechnung rechnung);
    Task SetPdfVerknuepfungAsync(
        int id,
        RechnungsPdfVerknuepfung verknuepfung,
        DateTime erwartetGeaendertAm,
        RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung);
    Task DeleteAsync(int id);
    Task DeleteIfUnchangedAsync(
        int id,
        DateTime erwartetGeaendertAm,
        RechnungsPdfVerknuepfung? erwartetePdfVerknuepfung);
}
