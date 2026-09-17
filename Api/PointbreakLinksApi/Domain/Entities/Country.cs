using Domain.Common;

namespace Domain.Entities;

public class Country : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // ISO-2, e.g. "kz", "ru"

    public ICollection<Site> Sites { get; set; } = [];
}
