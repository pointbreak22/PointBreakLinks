using Application.CQRS.Support.DTOs;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Support.Commands.SendSupportMessage;

public class SendSupportMessageCommandHandler(
    ISupportTicketRepository ticketRepository,
    ISupportMessageRepository messageRepository,
    IUserRepository userRepository)
    : IRequestHandler<SendSupportMessageCommand, SupportMessageDto>
{
    public async Task<SupportMessageDto> Handle(SendSupportMessageCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetOrCreateForUserAsync(request.UserId, cancellationToken);

        var message = new SupportMessage
        {
            SupportTicketId = ticket.Id,
            SenderId = request.UserId,
            IsFromStaff = false,
            Text = request.Text,
        };

        await messageRepository.AddAsync(message, cancellationToken);
        await messageRepository.SaveChangesAsync(cancellationToken);

        var sender = await userRepository.GetByIdWithRolesAndProjectsAsync(request.UserId, cancellationToken);
        return new SupportMessageDto(message.Id, message.SupportTicketId, message.SenderId, sender?.Name ?? string.Empty, false, message.Text, message.CreatedAt.ToString("dd.MM.yyyy HH:mm"), false);
    }
}
