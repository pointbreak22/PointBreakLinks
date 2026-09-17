using Application.Common;
using Application.CQRS.Support.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Support.Commands.SendSupportReply;

public class SendSupportReplyCommandHandler(
    ISupportTicketRepository ticketRepository,
    ISupportMessageRepository messageRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher)
    : IRequestHandler<SendSupportReplyCommand, SupportMessageDto>
{
    public async Task<SupportMessageDto> Handle(SendSupportReplyCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
                     ?? throw new NotFoundException("SupportTicket", request.TicketId);

        var message = new SupportMessage
        {
            SupportTicketId = ticket.Id,
            SenderId = request.StaffUserId,
            IsFromStaff = true,
            Text = request.Text,
        };

        await messageRepository.AddAsync(message, cancellationToken);
        await messageRepository.SaveChangesAsync(cancellationToken);

        var staff = await userRepository.GetByIdWithRolesAndProjectsAsync(request.StaffUserId, cancellationToken);
        var dto = new SupportMessageDto(message.Id, message.SupportTicketId, message.SenderId, staff?.Name ?? "Поддержка", true, message.Text, message.CreatedAt.ToString("dd.MM.yyyy HH:mm"), false);

        await notificationRepository.AddAsync(
            new Notification { UserId = ticket.UserId, Message = "Ответ от службы поддержки" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await notificationPusher.NotifyNewSupportMessageAsync(ticket.UserId, dto, cancellationToken);

        return dto;
    }
}
