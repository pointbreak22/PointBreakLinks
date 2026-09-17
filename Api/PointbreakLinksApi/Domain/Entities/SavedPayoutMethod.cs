using Domain.Common;

namespace Domain.Entities;

// A user's own address book of withdrawal destinations — purely a convenience so
// WithdrawalRequest.PayoutDetails doesn't need retyping every time (see that entity's comment
// for why it's still free text: no real payment processor exists to validate a "real" method
// against). Selecting one just fills the text field on the client; no FK from WithdrawalRequest
// back to this table.
public class SavedPayoutMethod : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
