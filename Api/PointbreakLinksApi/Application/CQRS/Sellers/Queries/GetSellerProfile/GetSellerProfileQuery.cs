using Application.CQRS.Sellers.DTOs;
using MediatR;

namespace Application.CQRS.Sellers.Queries.GetSellerProfile;

public record GetSellerProfileQuery(int SellerId) : IRequest<SellerProfileDto>;
