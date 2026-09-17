using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Services;

// Maps a hub connection to our own user id (the JWT "sub" claim) so Clients.User(id) can target
// a specific connected user (see INotificationPusher/SignalRNotificationPusher). Checking only
// "sub" silently matched nobody: ASP.NET Core's JWT handler remaps the registered "sub" claim to
// ClaimTypes.NameIdentifier by default before building the ClaimsPrincipal, the same reason
// ApiControllerBase.GetCurrentUserId() already falls back to it — this provider needs the same
// fallback, and didn't have it until real end-to-end testing of the first pushed notification
// (RequestPublicationCommandHandler's OrderReceived) showed nothing ever arrived client-side.
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("sub")?.Value ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
