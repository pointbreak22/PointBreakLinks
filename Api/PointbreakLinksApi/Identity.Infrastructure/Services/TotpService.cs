using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;

namespace Identity.Infrastructure.Services;

// RFC 4226 (HOTP) + RFC 6238 (TOTP) implemented directly against HMACSHA1 — no external NuGet
// package needed, the algorithm is small and completely standard (every authenticator app
// implements exactly this). 6-digit codes, 30-second steps, ±1 step tolerance for clock drift
// between the server and the user's phone.
public class TotpService : ITotpService
{
    private const int SecretLengthBytes = 20; // 160 bits — the RFC 4226-recommended HMAC-SHA1 key size
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int AllowedDriftSteps = 1;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(SecretLengthBytes));

    public string BuildOtpAuthUri(string secret, string accountEmail, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedLabel = Uri.EscapeDataString($"{issuer}:{accountEmail}");
        return $"otpauth://totp/{encodedLabel}?secret={secret}&issuer={encodedIssuer}&digits={Digits}&period={PeriodSeconds}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits || !code.All(char.IsAsciiDigit))
        {
            return false;
        }

        var secretBytes = Base32Decode(secret);
        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / PeriodSeconds;

        for (var drift = -AllowedDriftSteps; drift <= AllowedDriftSteps; drift++)
        {
            if (ComputeCode(secretBytes, currentStep + drift) == code)
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeCode(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        var hash = HMACSHA1.HashData(secret, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                          | ((hash[offset + 1] & 0xFF) << 16)
                          | ((hash[offset + 2] & 0xFF) << 8)
                          | (hash[offset + 3] & 0xFF);

        var code = binaryCode % (int)Math.Pow(10, Digits);
        return code.ToString().PadLeft(Digits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        var result = new StringBuilder();
        int bits = 0, value = 0;

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                result.Append(Base32Alphabet[(value >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            result.Append(Base32Alphabet[(value << (5 - bits)) & 0x1F]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var normalized = input.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        int bits = 0, value = 0;

        foreach (var c in normalized)
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0)
            {
                continue;
            }

            value = (value << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return [.. output];
    }
}
