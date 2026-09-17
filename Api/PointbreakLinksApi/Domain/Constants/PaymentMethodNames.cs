namespace Domain.Constants;

// Recorded on a top-up's BalanceTransaction purely for bookkeeping — no real payment provider
// is wired in anywhere (see TopUpBalanceCommandHandler's comment), so choosing one of these
// doesn't change how the money moves today, only what gets logged. Kept as two options rather
// than a free-form string because a real gateway integration later would route each one down a
// genuinely different processor (an international card network vs a Russia-only one can't share
// one merchant account), so the distinction is real even before there's a provider behind it.
public static class PaymentMethodNames
{
    public const string VisaMastercard = "visa_mastercard";
    public const string Mir = "mir";

    public static readonly IReadOnlyList<string> All = [VisaMastercard, Mir];
}
