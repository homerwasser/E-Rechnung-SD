using System.Threading.Tasks;

namespace ERechnung.Core.Services;

/// <summary>
/// Basisinterface fuer Repositorys.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}