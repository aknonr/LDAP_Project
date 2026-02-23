using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

/// <summary>
/// Job ve hedefleri icin veri erisim sozlesmesi.
/// </summary>
public interface IJobRepository
{
    /// <summary>
    /// Yeni job kaydi olusturur.
    /// </summary>
    Task AddAsync(Job job, CancellationToken cancellationToken);

    /// <summary>
    /// Job bilgisini ID ile getirir.
    /// </summary>
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Job erisim kontrolu icin gerekli minimum bilgileri getirir.
    /// </summary>
    Task<JobAccessInfo?> GetAccessInfoAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Job hedef listesini getirir (opsiyonel sayfalama).
    /// </summary>
    Task<IReadOnlyList<JobTarget>> GetTargetsAsync(Guid jobId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Job hedef toplam sayisini getirir.
    /// </summary>
    Task<int> GetTargetCountAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Job hedeflerini durum bazinda sayar.
    /// </summary>
    Task<int> CountTargetsByStatusAsync(Guid jobId, TargetStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// Yapilan degisiklikleri kalici hale getirir.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
