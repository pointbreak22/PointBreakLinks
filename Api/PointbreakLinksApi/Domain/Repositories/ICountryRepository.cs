using Domain.Entities;

namespace Domain.Repositories;

public interface ICountryRepository
{
    Task<List<Country>> GetAllAsync(CancellationToken cancellationToken = default);
}
