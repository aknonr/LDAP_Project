namespace Application.Abstractions.Repositories;

/// <summary>
/// Job erisim kontrolu icin gerekli minimum bilgiler.
/// </summary>
public sealed record JobAccessInfo
{
    public Guid JobId { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public string RequestedBySubject { get; init; } = string.Empty;
    public Guid? ServerGroupId { get; init; }
}

