using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Hubs;

// Placeholder hub — not wired to any feature yet. Once the Sites/PurchasedSites module
// exists, push order-status changes here (publication requested/confirmed), and once
// Messages exists, push new chat messages here too. See Application/CQRS/*/​_NEXT.md.
[Authorize]
public class NotificationHub : Hub;
