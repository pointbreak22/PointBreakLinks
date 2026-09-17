using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Common;

// Shared by Confirm2FaCommandHandler (initial mint, once TOTP setup is confirmed) and
// RegenerateBackupCodesCommandHandler. Codes are high-entropy (8 chars from a 32-symbol
// alphabet, no ambiguous 0/O or 1/I) single-use recovery tokens, so a fast SHA-256 hash is fine
// here — same reasoning TwoFactorLoginTicket.TokenHash already uses, unlike IPasswordHasher's
// slow hash which exists specifically for low-entropy human-chosen passwords.
public static class BackupCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeCount = 10;
    private const int CharsPerCode = 8;

    public static IReadOnlyList<string> GenerateCodes()
    {
        var codes = new List<string>(CodeCount);
        for (var i = 0; i < CodeCount; i++)
        {
            codes.Add(GenerateOne());
        }
        return codes;
    }

    private static string GenerateOne()
    {
        Span<char> chars = stackalloc char[CharsPerCode];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return $"{new string(chars[..4])}-{new string(chars[4..])}";
    }

    // Applied to both the freshly generated code (before hashing for storage) and whatever the
    // user later types back in, so "abcd-1234", "ABCD1234" and "ABCD-1234" all match the same hash.
    public static string Hash(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
