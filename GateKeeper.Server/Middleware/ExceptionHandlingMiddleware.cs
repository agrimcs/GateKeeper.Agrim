using System.Net;
using System.Text.Json;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Application.Common.Exceptions;
using FluentValidation;

namespace GateKeeper.Server.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Converts domain and application exceptions to proper HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, exception.Message),
            GateKeeper.Application.Common.Exceptions.ApplicationException => (HttpStatusCode.BadRequest, exception.Message),
            FluentValidation.ValidationException validationEx => (HttpStatusCode.BadRequest, 
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred processing your request")
        };

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
