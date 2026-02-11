namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job durum sorgusu girdisi.
/// </summary>
public sealed record GetJobStatusInput
{
    public Guid JobId { get; init; }
}
