using ConsoleApp.Models;

namespace ConsoleApp.Access;
public interface IStorageRepository
{
    Task<Source> AddAsync(Source entity, CancellationToken cancellation = default);

    Task<ICollection<Source>> GetAllAsync(CancellationToken cancellation = default);
}
