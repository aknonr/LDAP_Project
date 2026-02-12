using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;

namespace Infrastructure.Update.Strategies;

public sealed class IisVirtualDirUpdateStrategy : IUpdateStrategy
{
    public ResourceType ResourceType => ResourceType.IISVirtualDir;

    public Task<OperationResult> UpdateAsync(UpdateContext context, Domain.Entities.JobResource resource, CancellationToken cancellationToken)
    {
        // IIS virtual directory update burada uygulanacak.
        return Task.FromResult(OperationResult.Failure("UNKNOWN", "Update engine henuz entegre edilmedi."));
    }
}
