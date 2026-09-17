using Domain.Common;

namespace Domain.Entities;

public class Topic : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Site> Sites { get; set; } = [];
}
