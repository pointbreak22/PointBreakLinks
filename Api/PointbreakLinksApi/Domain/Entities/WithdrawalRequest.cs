using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

// The withdrawal-side counterpart to TopUpBalanceCommandHandler's honest gap: no real payment
// processor is integrated, so a top-up credits instantly — a withdrawal can't do the mirror
// image (debit instantly and mail cash), so instead this models the real-world manual process a
// small marketplace without payment-provider payouts would actually use: the user requests a
// payout to some off-platform destination they describe themselves (PayoutDetails — a card
// number, a bank requisite, whatever), an admin sends it manually outside the app, then marks
// the request Approved here. Amount is debited from Wallet.Balance the moment the request is
// created (RequestWithdrawalCommandHandler), same as Purchase — Rejected reverses that with a
// Refund transaction, same reuse ResolveDisputeCommandHandler/CancelOrderCommandHandler already
// established for "money comes back."
public class WithdrawalRequest : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }
    public string PayoutDetails { get; set; } = string.Empty;
    public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public string? AdminComment { get; set; }
}
