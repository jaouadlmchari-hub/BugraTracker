using BugTracker.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    ex,
                    "La réponse HTTP a déjà commencé. Method: {Method}, Path: {Path}",
                    context.Request.Method,
                    context.Request.Path);

                throw;
            }

            // 1. Déterminer le Status Code
            var statusCode = GetStatusCode(ex);

            // 2. Logger l'exception
            LogException(ex, statusCode, context);

            // 3. Ne pas exposer les détails internes des erreurs 500
            var detail = statusCode >= 500
                ? "Une erreur interne s'est produite."
                : ex.Message;

            // 4. Construire la réponse standard RFC 7807
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = detail,
                Instance = context.Request.Path
            };

            // 5. Écrire la réponse HTTP
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status422UnprocessableEntity => "Business Rule Violation",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            _ => "Internal Server Error"
        };
    }

    private void LogException(Exception exception, int statusCode, HttpContext context)
    {
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Erreur inattendue. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                statusCode);
        }
        else
        {
            _logger.LogWarning(
                "Erreur applicative. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Message: {Message}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                exception.Message);
        }
    }
}