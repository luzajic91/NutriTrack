using Serilog.Context;

namespace NutriTrack.Api;

/// <summary>
/// Assigns a correlation id to every request: honours an incoming
/// <c>X-Correlation-ID</c> header or generates one. The id is pushed into the
/// Serilog <see cref="LogContext"/> so all downstream logs carry it, stored on
/// <see cref="HttpContext.Items"/> for other middleware, and echoed back on the
/// response so clients (and error bodies) can reference it.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

/// <summary>Helper for reading the current request's correlation id.</summary>
public static class CorrelationIdExtensions
{
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var id)
            ? id?.ToString()
            : null;
}
