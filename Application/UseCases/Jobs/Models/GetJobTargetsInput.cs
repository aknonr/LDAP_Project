namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job hedef listesi sorgusu girdisi.
/// </summary>
public sealed record GetJobTargetsInput
{
    public Guid JobId { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 200;
}
