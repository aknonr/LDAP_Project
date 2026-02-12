namespace Application.Abstractions.Auditing;

/// <summary>
/// Audit kaydi yazma sozlesmesi.
/// </summary>
public interface IAuditTrailWriter
{
    /// <summary>
    /// Yeni audit kaydi ekler.
    /// </summary>
    Task WriteAsync(AuditEntryWriteModel entry, CancellationToken cancellationToken);
}
