using Domain.Common;

namespace Domain.Entities;

// One row per user — Balance used to live directly on User, but moved here once Identity
// became its own module: balance is a marketplace concern (what a user can spend/has earned
// on this platform), not an identity one, and keeping it on the Identity-owned User table
// would have meant the business side needing write access to a column Identity is supposed to
// own. Lazily created on first access (IWalletRepository.GetOrCreateAsync) rather than at
// registration time, since Registration lives in Identity now and has no reason to know Wallet
// exists.
public class Wallet : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Balance { get; set; }
}
