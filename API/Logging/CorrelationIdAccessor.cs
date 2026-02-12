namespace API.Logging;

/// <summary>
/// CorrelationId degerini pipeline genelinde standartlastirir.
/// </summary>
public static class CorrelationIdAccessor
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public static string? Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var item) && item is string itemValue)
        {
            return itemValue;
        }

        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return headerValue.ToString();
        }

        return context.TraceIdentifier;
    }
}
