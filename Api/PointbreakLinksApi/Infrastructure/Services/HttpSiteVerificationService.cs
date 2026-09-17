using System.Net;
using System.Net.Sockets;
using System.Text;
using Application.Common;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

// SSRF-hardened by design: the URL being fetched is a seller-supplied Site.Url, not a trusted
// constant, so a naive fetch would let a malicious seller point this server at internal
// infrastructure (cloud metadata endpoints, localhost, a private-network admin panel) and read
// the response back through the verification result. Every hostname actually contacted —
// including ones reached via redirect — is DNS-resolved and checked against
// private/loopback/link-local/multicast ranges before a request is sent to it; redirects are
// followed manually (HttpClient's own auto-redirect is disabled) specifically so each hop gets
// that same check instead of only the original URL.
public class HttpSiteVerificationService(IHttpClientFactory httpClientFactory, ILogger<HttpSiteVerificationService> logger) : ISiteVerificationService
{
    public const string HttpClientName = nameof(HttpSiteVerificationService);

    private const int MaxRedirects = 3;
    private const int MaxResponseBytes = 256 * 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public async Task<bool> VerifyAsync(string url, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var current = NormalizeUrl(url);
        if (current == null)
        {
            return false;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            if (!await IsSafeHostAsync(current.Host, timeoutCts.Token))
            {
                logger.LogWarning("Site verification refused unsafe host {Host}", current.Host);
                return false;
            }

            var client = httpClientFactory.CreateClient(HttpClientName);

            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, current);
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogInformation(ex, "Site verification request failed for {Url}", current);
                return false;
            }

            using (response)
            {
                if (IsRedirect(response.StatusCode))
                {
                    var location = response.Headers.Location;
                    if (location == null)
                    {
                        return false;
                    }

                    current = location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (current.Scheme != Uri.UriSchemeHttp && current.Scheme != Uri.UriSchemeHttps)
                    {
                        return false;
                    }

                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var html = await ReadCappedAsync(response, timeoutCts.Token);
                return html.Contains(token, StringComparison.Ordinal);
            }
        }

        return false;
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static async Task<string> ReadCappedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaxResponseBytes];
        var totalRead = 0;
        int read;
        while (totalRead < buffer.Length &&
               (read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken)) > 0)
        {
            totalRead += read;
        }

        return Encoding.UTF8.GetString(buffer, 0, totalRead);
    }

    private static Uri? NormalizeUrl(string url)
    {
        var candidate = url.Contains("://", StringComparison.Ordinal) ? url : $"https://{url}";
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }

    private static async Task<bool> IsSafeHostAsync(string host, CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] switch
            {
                0 => false, // 0.0.0.0/8
                10 => false, // 10.0.0.0/8
                127 => false, // 127.0.0.0/8
                169 when b[1] == 254 => false, // 169.254.0.0/16 — link-local, incl. cloud metadata (169.254.169.254)
                172 when b[1] is >= 16 and <= 31 => false, // 172.16.0.0/12
                192 when b[1] == 168 => false, // 192.168.0.0/16
                _ => true,
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return false;
            }

            var b = address.GetAddressBytes();
            return (b[0] & 0xFE) != 0xFC; // fc00::/7 — unique local addresses
        }

        return false;
    }
}
