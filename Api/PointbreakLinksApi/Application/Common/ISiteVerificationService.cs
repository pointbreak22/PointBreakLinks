namespace Application.Common;

// New, not from FOXLinks. Proves a seller actually controls the domain they're listing — the
// token must appear somewhere in the page returned by `url` (a meta tag or a plain text file
// both work; this just does a substring search, not strict HTML parsing). Implemented in
// Infrastructure with SSRF hardening since the URL is attacker-controlled input by design (any
// seller can type any URL) — see HttpSiteVerificationService's comment for what that means in
// practice.
public interface ISiteVerificationService
{
    Task<bool> VerifyAsync(string url, string token, CancellationToken cancellationToken = default);
}
