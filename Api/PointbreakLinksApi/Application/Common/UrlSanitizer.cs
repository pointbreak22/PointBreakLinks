using System.Text.RegularExpressions;

namespace Application.Common;

// Strips a leading http(s):// and a trailing slash — same normalization FOXLinks applies both
// client-side (add-edit-site-modal.vue) and server-side (SiteStoreDTO::fromRequest).
public static partial class UrlSanitizer
{
    public static string Sanitize(string url) => UrlProtocolPrefix().Replace(url.Trim(), string.Empty).TrimEnd('/');

    [GeneratedRegex(@"^https?://", RegexOptions.IgnoreCase)]
    private static partial Regex UrlProtocolPrefix();
}
