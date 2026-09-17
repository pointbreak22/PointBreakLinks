using Application.CQRS.Messages.DTOs;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetMessageAttachment;

public record GetMessageAttachmentQuery(int PurchasedSiteId, int MessageId, int ViewerId) : IRequest<MessageAttachmentDto>;
