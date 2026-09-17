using Domain.Entities;

namespace Domain.Repositories;

public interface ITopicRepository
{
    Task<List<Topic>> GetAllAsync(CancellationToken cancellationToken = default);
}
