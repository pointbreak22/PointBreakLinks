using System.Globalization;
using Application.CQRS.Admin.Queries.GetUsers;
using Domain.Repositories;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers;

// Backs the admin sidebar's "Отчёты" link (previously coming-soon). Three flat CSV exports over
// real data, same CsvWriter/pattern SitesController's sales/project-sites exports already use —
// no invented numbers, no charts.
[Authorize(Roles = RoleNames.Admin)]
[Authorize(Policy = "RequireTwoFactor")]
[Route("api/admin/reports")]
public class AdminReportsController(
    IMediator mediator,
    IPurchasedSiteRepository purchasedSiteRepository,
    IBalanceTransactionRepository transactionRepository,
    IWithdrawalRequestRepository withdrawalRequestRepository) : ApiControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> ExportOrders(CancellationToken cancellationToken)
    {
        var rows = await purchasedSiteRepository.GetAllForExportAsync(cancellationToken);

        var csv = CsvWriter.Write(
            ["ID", "Площадка", "Покупатель", "Продавец", "Сумма", "Статус", "Дата"],
            rows.Select(r => (IReadOnlyList<string>)
            [
                r.Id.ToString(),
                r.SiteUrl,
                r.BuyerName,
                r.SellerName,
                r.FinalPrice.ToString("F2", CultureInfo.InvariantCulture),
                r.Status,
                r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            ]));

        return File(csv, "text/csv", "orders.csv");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> ExportTransactions(CancellationToken cancellationToken)
    {
        var rows = await transactionRepository.GetAllForExportAsync(cancellationToken);

        var csv = CsvWriter.Write(
            ["ID", "Пользователь", "Тип", "Сумма", "Описание", "Дата"],
            rows.Select(r => (IReadOnlyList<string>)
            [
                r.Id.ToString(),
                r.UserName,
                r.Type,
                r.Amount.ToString("F2", CultureInfo.InvariantCulture),
                r.Description,
                r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            ]));

        return File(csv, "text/csv", "transactions.csv");
    }

    [HttpGet("users")]
    public async Task<IActionResult> ExportUsers(CancellationToken cancellationToken)
    {
        var users = await mediator.Send(new GetUsersQuery(1, int.MaxValue), cancellationToken);

        var csv = CsvWriter.Write(
            ["ID", "Имя", "Email", "Роли", "Проектов", "Забанен", "Дата регистрации"],
            users.Items.Select(u => (IReadOnlyList<string>)
            [
                u.Id.ToString(),
                u.Name,
                u.Email,
                string.Join("; ", u.Roles.Select(r => r.DisplayName)),
                u.ProjectsCount.ToString(),
                u.IsBanned ? "Да" : "Нет",
                u.CreatedAt,
            ]));

        return File(csv, "text/csv", "users.csv");
    }

    [HttpGet("withdrawals")]
    public async Task<IActionResult> ExportWithdrawals(CancellationToken cancellationToken)
    {
        var rows = await withdrawalRequestRepository.GetAllForExportAsync(cancellationToken);

        var csv = CsvWriter.Write(
            ["ID", "Пользователь", "Email", "Сумма", "Реквизиты", "Статус", "Подана", "Обработана", "Комментарий"],
            rows.Select(r => (IReadOnlyList<string>)
            [
                r.Id.ToString(),
                r.UserName,
                r.UserEmail,
                r.Amount.ToString("F2", CultureInfo.InvariantCulture),
                r.PayoutDetails,
                WithdrawalStatusLabels.GetValueOrDefault(r.Status, r.Status),
                r.RequestedAt.ToString("dd.MM.yyyy HH:mm"),
                r.ProcessedAt?.ToString("dd.MM.yyyy HH:mm") ?? "",
                r.AdminComment ?? "",
            ]));

        return File(csv, "text/csv", "withdrawals.csv");
    }

    private static readonly Dictionary<string, string> WithdrawalStatusLabels = new()
    {
        ["Pending"] = "Ожидает",
        ["Approved"] = "Одобрено",
        ["Rejected"] = "Отклонено",
    };
}
