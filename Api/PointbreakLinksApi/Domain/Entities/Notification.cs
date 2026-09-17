using Domain.Common;

namespace Domain.Entities;

// New entity, not from FOXLinks. Persists the same events INotificationPusher already pushes
// live over SignalR — this is the durable side (an inbox you can read later / while offline),
// that one is the ephemeral side (a toast, only seen if connected right now). Deliberately two
// separate concerns/interfaces rather than one merged abstraction — see
// Application/Common/INotificationPusher.cs vs INotificationRepository.cs.
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
