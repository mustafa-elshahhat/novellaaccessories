using System.Text.Json;
using Novella.Api.Common;
using Novella.Application.Common;

namespace Novella.Api.Middleware;

/// <summary>
/// Translates exceptions into the standard error model. Known <see cref="AppException"/>s map to
/// their code/status; unknown exceptions return a generic 500 without leaking internals. Never
/// logs sensitive values (OTP codes, passwords, tokens, secrets are not part of exception data).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, new ApiError { Code = ex.Code, Message = ex.Message, Details = ex.Details });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, 500, new ApiError
            {
                Code = ErrorCodes.Internal,
                Message = "An unexpected error occurred."
            });
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, ApiError error)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
