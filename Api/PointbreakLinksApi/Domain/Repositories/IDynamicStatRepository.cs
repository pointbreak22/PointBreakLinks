using Domain.Entities;

namespace Domain.Repositories;

public interface IDynamicStatRepository
{
    Task<List<DynamicStat>> GetByPageKeyAsync(string pageKey, CancellationToken cancellationToken = default);
}
