using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;

namespace Infrastructure.Update.Strategies;

public sealed class ServiceUpdateStrategy : IUpdateStrategy
{
    public ResourceType ResourceType => ResourceType.Service;

    public Task<OperationResult> UpdateAsync(UpdateContext context, Domain.Entities.JobResource resource, CancellationToken cancellationToken)
    {
        // Servis hesabini guncelleme burada uygulanacak.
        return Task.FromResult(OperationResult.Failure("UNKNOWN", "Update engine henuz entegre edilmedi."));
    }
}
