using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetCountries;

public class GetCountriesQueryHandler(ICountryRepository countryRepository) : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        var countries = await countryRepository.GetAllAsync(cancellationToken);
        return countries.Select(c => new CountryDto(c.Id, c.Code, c.Name)).ToList();
    }
}
