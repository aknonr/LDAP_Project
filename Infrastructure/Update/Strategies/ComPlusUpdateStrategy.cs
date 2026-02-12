using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;

namespace Infrastructure.Update.Strategies;

public sealed class ComPlusUpdateStrategy : IUpdateStrategy
{
    public ResourceType ResourceType => ResourceType.COMPlus;

    public Task<OperationResult> UpdateAsync(UpdateContext context, Domain.Entities.JobResource resource, CancellationToken cancellationToken)
    {
        // COM+ update burada uygulanacak.
        return Task.FromResult(OperationResult.Failure("UNKNOWN", "Update engine henuz entegre edilmedi."));
    }
}
