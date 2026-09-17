using Application.CQRS.Support.DTOs;
using MediatR;

namespace Application.CQRS.Support.Commands.SendSupportReply;

public record SendSupportReplyCommand(int TicketId, int StaffUserId, string Text) : IRequest<SupportMessageDto>;
