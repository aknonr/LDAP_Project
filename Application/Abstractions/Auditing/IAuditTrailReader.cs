namespace Application.Abstractions.Auditing;

/// <summary>
/// Audit kaydi okuma sozlesmesi.
/// </summary>
public interface IAuditTrailReader
{
    /// <summary>
    /// En son audit kayitlarini getirir.
    /// </summary>
    Task<IReadOnlyList<AuditEntryViewModel>> GetRecentAsync(
        int take,
        string? correlationId,
        CancellationToken cancellationToken);
}
