using Domain.Entities;

namespace Application.Tests;

// Minimal-but-valid entity graphs for handler tests — just enough navigation properties
// populated to satisfy SiteDto.FromEntity/PurchasedSiteDto.FromEntity (both throw
// NullReferenceException on Topic/Status/Site if left at their `= null!` defaults) without
// dragging EF Core or a real DbContext into these tests.
internal static class TestBuilders
{
    public static Site Site(int id = 1, int sellerId = 10, decimal price = 1000m, string url = "example.ru") => new()
    {
        Id = id,
        Url = url,
        SellerId = sellerId,
        Price = price,
        TopicId = 1,
        Topic = new Topic { Id = 1, Name = "Тема" },
        StatusId = 2,
        Status = new Status { Id = 2, Name = "moderation", Description = "На модерации" },
        VerificationToken = "token",
    };

    public static PurchasedSite Order(int id = 1, int buyerId = 20, int statusId = 4, string statusName = "work", decimal finalPrice = 1000m, Site? site = null) => new()
    {
        Id = id,
        BuyerId = buyerId,
        Buyer = new User { Id = buyerId, Name = "Покупатель" },
        ProjectId = 1,
        SiteId = site?.Id ?? 1,
        Site = site ?? Site(),
        StatusId = statusId,
        Status = new Status { Id = statusId, Name = statusName, Description = statusName },
        FinalPrice = finalPrice,
    };

    public static Wallet Wallet(int userId = 20, decimal balance = 0m) => new() { UserId = userId, Balance = balance };
}
