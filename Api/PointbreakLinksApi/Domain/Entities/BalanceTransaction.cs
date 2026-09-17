using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

// A ledger row, not just a running total on User.Balance — every balance change (top-up,
// purchase, sale) leaves a permanent record with what it was for, so "История операций" on the
// wallet page is a real audit trail, not derived/guessed after the fact.
public class BalanceTransaction : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public BalanceTransactionType Type { get; set; }

    // Positive for TopUp/Sale, negative for Purchase — so a running balance is just a sum,
    // and the sign alone tells the wallet-page table which color/icon to use.
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    // Set for Purchase/Sale rows so a transaction can link back to the order that caused it;
    // null for TopUp.
    public int? PurchasedSiteId { get; set; }
    public PurchasedSite? PurchasedSite { get; set; }

    // Set for TopUp rows only — see Domain/Constants/PaymentMethodNames.cs.
    public string? PaymentMethod { get; set; }
}
