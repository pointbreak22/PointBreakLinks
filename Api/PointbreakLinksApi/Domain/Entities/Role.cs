using Domain.Common;

namespace Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
