using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public interface IRechnungRepository
{
    Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null);
    Task<Rechnung?> GetByIdAsync(int id);
    Task<Rechnung> CreateAsync(Rechnung rechnung);
    Task UpdateAsync(Rechnung rechnung);
    Task DeleteAsync(int id);
}
