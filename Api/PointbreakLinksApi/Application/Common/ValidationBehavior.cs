using FluentValidation;
using MediatR;

namespace Application.Common;

// Runs every registered IValidator<TRequest> before the handler executes, so a command with
// invalid input never reaches business logic at all. Deliberately scoped to pure input-shape
// checks (string length, required fields, numeric ranges) — rules that need a database lookup
// (duplicate email, insufficient balance, order-status gates) stay in the handler, since a
// validator hitting the DB would duplicate round-trips and blur the CQRS boundary this codebase
// otherwise keeps clean. FluentValidation.ValidationException is caught by DomainExceptionHandler
// (matched by type name, same as ConflictException/NotFoundException) and mapped to 400.
// Registered ONCE, globally, in WebAPI's Program.cs — MediatR's pipeline spans both the business
// Application assembly and Identity.Application (see AddMediatR's RegisterServicesFromAssemblies),
// so this single behavior + AddValidatorsFromAssembly for both assemblies covers every command
// regardless of which bounded context it belongs to. No need for a second copy in
// Identity.Application: doing so would run validation twice per request.
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
