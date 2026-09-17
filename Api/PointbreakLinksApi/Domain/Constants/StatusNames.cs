namespace Domain.Constants;

// Ported 1:1 from FOXLinks' StatusSeeder. One shared lookup table doing double duty as both
// Site moderation status and PurchasedSite order status — see Domain/Entities/Status.cs.
public static class StatusNames
{
    public const string Application = "application"; // Заявка — initial PurchasedSite status
    public const string Moderation = "moderation"; // На модерации — initial Site status
    public const string Rejected = "rejected";
    public const string Work = "work"; // в работе — PurchasedSite accepted by the seller
    public const string Paid = "paid";

    // Added this session, not from FOXLinks' StatusSeeder — a moderator approving a Site needs
    // somewhere real to land it; none of the five seeded above fit ("work"/"paid" are
    // order-lifecycle names). See Application/CQRS/Moderation.
    public const string Active = "active"; // Активна — Site approved by a moderator

    // Added this session — a PurchasedSite stuck in "application" forever (seller never
    // accepts) had no way out; "rejected" was already claimed by Site moderation on this same
    // shared lookup table, so this is a distinct value rather than overloading that one.
    // See Application/CQRS/Sites/Commands/CancelOrder and DeclineOrder.
    public const string Cancelled = "cancelled";
}
