using Domain.Common;

namespace Domain.Entities;

// A buyer's (optimizer's) project — groups purchased site placements and tracks aggregate spend.
public class Project : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }

    public bool HasLossInsurance { get; set; } // fl_loss_insurance
    public bool HasLossAndIndexationInsurance { get; set; } // fl_loss_and_indexation_insurance
    public string TaskForVm { get; set; } = string.Empty; // task_for_VM

    public int TotalLinks { get; set; } // total_number_of_links
    public int LinksPosted { get; set; } // number_of_links_posted
    public int FrozenPosted { get; set; } // number_of_frozen_posted

    public long SpentMoney { get; set; }

    public ICollection<PurchasedSite> PurchasedSites { get; set; } = [];
}
