using Domain.Enums;

namespace Application.Abstractions.Discovery;

/// <summary>
/// Discovery sonucunda bulunan kaynak.
/// </summary>
public sealed record DiscoveredResource
{
    public ResourceType ResourceType { get; init; }
    public string ResourceName { get; init; } = string.Empty;
    public string? ResourcePath { get; init; }
}
