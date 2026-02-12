using Serilog.Context;

namespace API.Logging;

/// <summary>
/// X-Correlation-Id degerini normalize edip log context'e tasir.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationIdAccessor.ItemKey] = correlationId;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdAccessor.HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdAccessor.HeaderName, out var headerValue))
        {
            var trimmed = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }

        return context.TraceIdentifier;
    }
}
