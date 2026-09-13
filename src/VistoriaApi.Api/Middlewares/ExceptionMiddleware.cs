using System.Text.Json;
using VistoriaApi.Application.Exceptions;
using VistoriaApi.Domain.Exceptions;

namespace VistoriaApi.Api.Middlewares;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Falha ao processar a requisição {RequestPath}",
                context.Request.Path);

            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            AppException applicationException => applicationException.StatusCode,
            DomainException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
        var message = statusCode == StatusCodes.Status500InternalServerError
            ? "Ocorreu um erro interno inesperado."
            : exception.Message;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = message,
            status = statusCode,
            traceId = context.TraceIdentifier
        }));
    }
}
