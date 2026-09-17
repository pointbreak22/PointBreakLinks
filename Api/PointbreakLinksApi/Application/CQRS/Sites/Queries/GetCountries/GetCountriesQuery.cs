using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetCountries;

public record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;
