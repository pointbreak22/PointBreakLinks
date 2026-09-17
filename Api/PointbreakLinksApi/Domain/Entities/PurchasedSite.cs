using Domain.Common;

namespace Domain.Entities;

// One order: a buyer's placement on a specific Site, within a Project.
public class PurchasedSite : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public int BuyerId { get; set; } // user_id_buyer
    public User Buyer { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public string? TaskDescription { get; set; }

    public int? PaymentSettingId { get; set; }
    public PaymentSetting? PaymentSetting { get; set; }

    public decimal FinalPrice { get; set; } // price_final

    public bool HasLinks { get; set; } // fl_has_links
    public bool IsPublicationRequested { get; set; }
    public bool IsPublished { get; set; }

    // A buyer's escalation on a "work"-status order (seller accepted but is stalling/
    // unresponsive) — see Application/CQRS/Sites/Commands/OpenDispute. Deliberately just a flag
    // + reason rather than a whole ticket entity: the existing order chat (Message) already
    // carries the back-and-forth; this only needs to mark "an admin should look at this" and
    // record why.
    public bool IsDisputed { get; set; }
    public string? DisputeReason { get; set; }

    public ICollection<Link> Links { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public SiteReview? Review { get; set; }
}
