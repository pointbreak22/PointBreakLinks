namespace Application.CQRS.Admin.DTOs;

public record AdminTransactionDto(int Id, string UserName, string Type, decimal Amount, string Description, string CreatedAt);
