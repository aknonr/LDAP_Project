using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;

namespace Infrastructure.Update.Strategies;

public sealed class IisAppPoolUpdateStrategy : IUpdateStrategy
{
    public ResourceType ResourceType => ResourceType.IISAppPool;

    public Task<OperationResult> UpdateAsync(UpdateContext context, Domain.Entities.JobResource resource, CancellationToken cancellationToken)
    {
        // IIS AppPool kimlik bilgisi guncellemesi burada uygulanacak.
        return Task.FromResult(OperationResult.Failure("UNKNOWN", "Update engine henuz entegre edilmedi."));
    }
}
