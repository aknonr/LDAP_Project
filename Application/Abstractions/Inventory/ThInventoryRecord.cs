using System.Text.Json.Serialization;

namespace Application.Abstractions.Inventory;

/// <summary>
/// TH API inventory kaydi.
/// </summary>
public sealed record ThInventoryRecord
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTimeOffset? CreatedDate { get; init; }

    [JsonPropertyName("updatedDate")]
    public DateTimeOffset? UpdatedDate { get; init; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("ip")]
    public string? IpAddress { get; init; }

    [JsonPropertyName("groupId")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; init; } = string.Empty;
}
