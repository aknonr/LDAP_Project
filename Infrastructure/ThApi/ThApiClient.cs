using Application.Abstractions.Inventory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Infrastructure.ThApi;

public sealed class ThApiClient : IThApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ThApiOptions> _options;

    public ThApiClient(HttpClient httpClient, IOptionsMonitor<ThApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<ThInventoryRecord>> GetInventoryAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException("ThApi BaseUrl config eksik.");
        }

        var endpoint = string.IsNullOrWhiteSpace(options.InventoryEndpoint)
            ? "/inventory"
            : options.InventoryEndpoint;
        var baseUri = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var requestUri = new Uri(baseUri, endpoint.TrimStart('/'));

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        ApplyAuthHeaders(request, options);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<ThInventoryRecord>>(stream, JsonOptions, cancellationToken);
        return items ?? new List<ThInventoryRecord>();
    }

    private static void ApplyAuthHeaders(HttpRequestMessage request, ThApiOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }

        if (!string.IsNullOrWhiteSpace(options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
        }
    }
}
