namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job olusturma sonuc bilgileri.
/// </summary>
public sealed record JobCreatedOutput
{
    public Guid JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public int TargetCount { get; init; }
}
