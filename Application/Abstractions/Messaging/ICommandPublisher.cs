using Application.Messaging.Commands;

namespace Application.Abstractions.Messaging;

/// <summary>
/// MQ uzerinden command gonderimi icin soyutlama.
/// </summary>
public interface ICommandPublisher
{
    /// <summary>
    /// Password-change job orkestrasyonunu baslatan komutu gonderir.
    /// </summary>
    Task PublishStartPasswordChangeJobAsync(StartPasswordChangeJobCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Discovery komutu gonderir.
    /// </summary>
    Task PublishDiscoveryAsync(DiscoverServerUsageCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Update komutu gonderir.
    /// </summary>
    Task PublishUpdateAsync(UpdateServerResourcesCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Verify komutu gonderir.
    /// </summary>
    Task PublishVerifyAsync(VerifyServerCommand command, CancellationToken cancellationToken);
}
