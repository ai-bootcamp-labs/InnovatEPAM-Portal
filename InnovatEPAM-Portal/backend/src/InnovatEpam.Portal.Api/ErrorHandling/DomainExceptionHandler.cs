using FluentValidation;
using InnovatEpam.Portal.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Api.ErrorHandling;

/// <summary>
/// Global <see cref="IExceptionHandler"/> that translates domain and
/// validation exceptions into RFC 7807 ProblemDetails responses (T026, R5).
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public DomainExceptionHandler(
        ILogger<DomainExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, errors) = Map(exception);
        if (status is null)
        {
            return false;
        }

        _logger.LogWarning(
            exception,
            "Domain exception {ExceptionType} mapped to HTTP {Status}",
            exception.GetType().Name,
            status);

        httpContext.Response.StatusCode = status.Value;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Type = $"https://httpstatuses.io/{status}",
            Instance = httpContext.Request.Path,
        };

        if (errors is { Count: > 0 })
        {
            problem.Extensions["errors"] = errors;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static (int? Status, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", null),
        // Phase 6/7 — surface a stable machine-readable `code` so the React
        // client can switch on it without parsing the title (FR-007, FR-009).
        SelfScoringForbiddenException => (
            StatusCodes.Status409Conflict,
            "Conflict",
            new Dictionary<string, string[]> { ["code"] = new[] { SelfScoringForbiddenException.Code } }),
        ScoringClosedException => (
            StatusCodes.Status409Conflict,
            "Conflict",
            new Dictionary<string, string[]> { ["code"] = new[] { ScoringClosedException.Code } }),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict", null),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", null),
        DomainValidationException dv => (StatusCodes.Status400BadRequest, "Validation failed", dv.Errors),
        ValidationException fv => (
            StatusCodes.Status400BadRequest,
            "Validation failed",
            fv.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict", null),
        _ => (null, string.Empty, null),
    };
}
