namespace Application.CQRS.Admin.DTOs;

public record AdminProjectDto(
    int Id,
    string Name,
    string OwnerName,
    string Type,
    int TotalLinks,
    int LinksPosted,
    long SpentMoney,
    string CreatedAt);
