using Application.CQRS.Support.DTOs;
using MediatR;

namespace Application.CQRS.Support.Commands.SendSupportMessage;

public record SendSupportMessageCommand(int UserId, string Text) : IRequest<SupportMessageDto>;
