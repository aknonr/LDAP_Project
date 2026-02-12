using Application.Abstractions.Auditing;
using Domain.Entities;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Auditing;

public sealed class AuditTrailStore : IAuditTrailWriter, IAuditTrailReader
{
    private const int MaxQueryTake = 500;
    private readonly AdpmDbContext _dbContext;

    public AuditTrailStore(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AuditEntryWriteModel entry, CancellationToken cancellationToken)
    {
        // Security gereksinimi icin audit verisi sanitize edilerek kaliciya yazilir.
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            Who = Sanitize(entry.Who, 200, "unknown"),
            When = entry.When == default ? DateTimeOffset.UtcNow : entry.When,
            TicketRef = Sanitize(entry.TicketRef, 100, null),
            TargetAccount = Sanitize(entry.TargetAccount, 200, "n/a"),
            ServerGroup = Sanitize(entry.ServerGroup, 200, "n/a"),
            ResultSummary = Sanitize(entry.ResultSummary, 1000, "n/a"),
            CorrelationId = Sanitize(entry.CorrelationId, 100, null)
        };

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryViewModel>> GetRecentAsync(
        int take,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        // SuperAdmin rapor ekrani icin son kayitlari filtreli/sinirli getirir.
        var normalizedTake = Math.Clamp(take, 1, MaxQueryTake);
        var normalizedCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? null
            : correlationId.Trim();

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(item => item.When)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedCorrelationId))
        {
            query = query.Where(item => item.CorrelationId == normalizedCorrelationId);
        }

        return await query
            .Take(normalizedTake)
            .Select(item => new AuditEntryViewModel
            {
                Id = item.Id,
                Who = item.Who,
                When = item.When,
                TicketRef = item.TicketRef,
                TargetAccount = item.TargetAccount,
                ServerGroup = item.ServerGroup,
                ResultSummary = item.ResultSummary,
                CorrelationId = item.CorrelationId
            })
            .ToListAsync(cancellationToken);
    }

    private static string Sanitize(string? value, int maxLength, string? fallback)
    {
        var redacted = SensitiveDataRedactor.Redact(value);
        if (string.IsNullOrWhiteSpace(redacted))
        {
            return fallback ?? string.Empty;
        }

        var trimmed = redacted.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
