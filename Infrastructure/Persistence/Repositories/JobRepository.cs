using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class JobRepository : IJobRepository
{
    private readonly AdpmDbContext _dbContext;

    public JobRepository(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken)
    {
        // Yeni job agacini (targets dahil) kayda alir.
        await _dbContext.Jobs.AddAsync(job, cancellationToken);
    }

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Job ozeti icin tek kayit okur.
        return _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
    }

    public async Task<IReadOnlyList<JobTarget>> GetTargetsAsync(Guid jobId, int skip, int take, CancellationToken cancellationToken)
    {
        // Hedefleri stabil sirayla sayfalar.
        return await _dbContext.JobTargets
            .AsNoTracking()
            .Where(target => target.JobId == jobId)
            .OrderBy(target => target.ServerName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetTargetCountAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return _dbContext.JobTargets
            .AsNoTracking()
            .CountAsync(target => target.JobId == jobId, cancellationToken);
    }

    public Task<int> CountTargetsByStatusAsync(Guid jobId, TargetStatus status, CancellationToken cancellationToken)
    {
        return _dbContext.JobTargets
            .AsNoTracking()
            .CountAsync(target => target.JobId == jobId && target.Status == status, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
