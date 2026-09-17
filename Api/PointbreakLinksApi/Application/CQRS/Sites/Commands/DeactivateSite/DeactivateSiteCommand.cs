using MediatR;

namespace Application.CQRS.Sites.Commands.DeactivateSite;

// FOXLinks routes DELETE /sites/{id} to a destroy() method that doesn't actually exist on
// SiteController (a dead route). A hard delete would also cascade-delete every PurchasedSite
// ever made against this listing (see Infrastructure's PurchasedSiteConfiguration), wiping
// order history — so this deactivates (fl_is_active = false) instead, which is what "remove my
// listing" should safely mean for a marketplace.
public record DeactivateSiteCommand(int SiteId, int OwnerId) : IRequest;
