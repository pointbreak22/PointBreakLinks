using Application.CQRS.Sellers.DTOs;
using Application.CQRS.Sellers.Queries.GetSellerProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// New module — no seller identity was clickable anywhere in the catalog before this (SiteDto
// carried a bare SellerId with no name attached). "Public" here matches the rest of the app:
// visible to any logged-in user, same as the catalog itself (SitesController is [Authorize]
// too) — not visible without an account.
[Authorize]
[Route("api/sellers")]
public class SellersController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SellerProfileDto>> GetProfile(int id) =>
        Ok(await mediator.Send(new GetSellerProfileQuery(id)));
}
