using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Middleware;

// Maps Domain exceptions to the right HTTP status, in one place, instead of every controller
// action repeating its own try/catch (AuthController still catches AuthenticationException
// itself since it needs a field-specific response shape; everything else relies on this).
// Matches by exception TYPE NAME rather than a type-pattern switch — the business Domain
// (Domain.Exceptions) and Identity.Domain (Identity.Domain.Exceptions) each define their own
// NotFoundException/ConflictException/AuthenticationException/ForbiddenException classes (two
// separate bounded contexts, see PROJECT_MAP.md), and this handler needs to recognize both
// without referencing Identity.Domain — WebAPI already does transitively, but Application
// deliberately doesn't, and this project mirrors that boundary.
// Anything NOT a recognized exception name falls through to ASP.NET Core's own problem-details
// handling (still registered via app.UseExceptionHandler() with no custom handlers before this
// one matched) — no stack trace ever reaches the client either way.
public class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // FluentValidation.ValidationException (thrown by Application.Common.ValidationBehavior
        // for bad input shape) gets its own branch — it carries multiple per-field failures,
        // which a flat ProblemDetails.Detail can't represent. ValidationProblemDetails.Errors is
        // there for exactly this; Detail is still populated with a joined summary so the
        // frontend's existing extractErrorMessage() (which only reads `.detail`) keeps working
        // without needing to special-case validation errors.
        if (exception is ValidationException validationException)
        {
            logger.LogInformation(exception, "Handled validation failure: {Message}", exception.Message);

            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage)),
                },
                cancellationToken);

            return true;
        }

        var (statusCode, title) = exception.GetType().Name switch
        {
            "NotFoundException" => (StatusCodes.Status404NotFound, "Not Found"),
            "ConflictException" => (StatusCodes.Status409Conflict, "Conflict"),
            "AuthenticationException" => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            "ForbiddenException" => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogInformation(exception, "Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = statusCode, Title = title, Detail = exception.Message },
            cancellationToken);

        return true;
    }
}
