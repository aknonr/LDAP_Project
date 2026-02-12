using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;

namespace Infrastructure.Update.Strategies;

public sealed class ScheduledTaskUpdateStrategy : IUpdateStrategy
{
    public ResourceType ResourceType => ResourceType.ScheduledTask;

    public Task<OperationResult> UpdateAsync(UpdateContext context, Domain.Entities.JobResource resource, CancellationToken cancellationToken)
    {
        // Scheduled task hesap guncellemesi burada uygulanacak.
        return Task.FromResult(OperationResult.Failure("UNKNOWN", "Update engine henuz entegre edilmedi."));
    }
}
