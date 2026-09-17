namespace Domain.Enums;

// Not from FOXLinks — balance-topup-modal.vue/notifications-modal.vue in the Nuxt source are
// both orphaned (never imported anywhere), and no `balance`/`wallet` column or table exists in
// any FOXLinks migration. Designed from scratch — see Domain/Entities/BalanceTransaction.cs.
public enum BalanceTransactionType
{
    TopUp = 0,
    Purchase = 1,
    Sale = 2,

    // Reverses a Purchase row when an order is cancelled/declined while still in "application"
    // status — see CancelOrderCommandHandler/DeclineOrderCommandHandler.
    Refund = 3,

    // Claws back a Sale row when an admin resolves a dispute in the buyer's favor after the
    // seller was already paid (see ResolveDisputeCommandHandler) — can legitimately push a
    // seller's Wallet.Balance negative, same as a real payment processor's chargeback; there's
    // no non-negative constraint on Wallet.Balance for exactly this reason.
    DisputeChargeback = 4,

    // Debited the moment a WithdrawalRequest is created (RequestWithdrawalCommandHandler), same
    // "hold the funds immediately" reasoning as Purchase — a rejected request reverses it with a
    // Refund row rather than a dedicated type, matching Cancel/DeclineOrder's own reuse of Refund.
    Withdrawal = 5,
}
