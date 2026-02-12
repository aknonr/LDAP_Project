using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Update;

/// <summary>
/// Kaynak tipine gore update stratejisi.
/// </summary>
public interface IUpdateStrategy
{
    ResourceType ResourceType { get; }

    /// <summary>
    /// Kaynak uzerinde parola guncellemesini uygular.
    /// </summary>
    Task<OperationResult> UpdateAsync(
        UpdateContext context,
        JobResource resource,
        CancellationToken cancellationToken);
}
