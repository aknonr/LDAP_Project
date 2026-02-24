using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Worker.Configuration;

namespace Worker.Observability;

public sealed class RabbitMqManagementQueueLagProbe : IQueueLagProbe
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<QueueLagOptions> _options;
    private readonly ILogger<RabbitMqManagementQueueLagProbe> _logger;

    public RabbitMqManagementQueueLagProbe(
        HttpClient httpClient,
        IOptionsMonitor<QueueLagOptions> options,
        ILogger<RabbitMqManagementQueueLagProbe> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<QueueLagSample?> TryReadAsync(
        string queueName,
        string virtualHost,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return null;
        }

        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return null;
        }

        if (!Uri.TryCreate(options.ManagementBaseUrl, UriKind.Absolute, out var baseUri))
        {
            _logger.LogWarning("Queue lag probe atlandi. ManagementBaseUrl gecersiz.");
            return null;
        }

        _httpClient.BaseAddress = baseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        _httpClient.DefaultRequestHeaders.Authorization = BuildBasicAuthHeader(options.Username, options.Password);

        var encodedVhost = Uri.EscapeDataString(string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost);
        var encodedQueue = Uri.EscapeDataString(queueName);
        var requestPath = $"api/queues/{encodedVhost}/{encodedQueue}";

        try
        {
            using var response = await _httpClient.GetAsync(requestPath, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Queue lag probe HTTP hatasi. Queue={Queue}, StatusCode={StatusCode}",
                    queueName,
                    (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var messagesReady = root.TryGetProperty("messages_ready", out var readyElement) ? readyElement.GetInt32() : 0;
            var messagesUnacked = root.TryGetProperty("messages_unacknowledged", out var unackedElement) ? unackedElement.GetInt32() : 0;

            return new QueueLagSample(
                queueName,
                messagesReady,
                messagesUnacked,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Queue lag probe exception. Queue={Queue}",
                queueName);
            return null;
        }
    }

    private static AuthenticationHeaderValue BuildBasicAuthHeader(string username, string password)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", token);
    }
}
