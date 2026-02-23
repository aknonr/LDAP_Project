using Application.Abstractions.Messaging;
using Application.Messaging.Commands;
using MassTransit;

namespace Infrastructure.Messaging;

public sealed class MassTransitCommandPublisher : ICommandPublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MassTransitCommandPublisher(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task PublishStartPasswordChangeJobAsync(StartPasswordChangeJobCommand command, CancellationToken cancellationToken)
    {
        // EndpointConvention map'ine gore password-change orchestrator komutunu gonderir.
        await _sendEndpointProvider.Send(command, cancellationToken);
    }

    public async Task PublishDiscoveryAsync(DiscoverServerUsageCommand command, CancellationToken cancellationToken)
    {
        // EndpointConvention map'ine gore discovery command gonderir.
        await _sendEndpointProvider.Send(command, cancellationToken);
    }

    public async Task PublishUpdateAsync(UpdateServerResourcesCommand command, CancellationToken cancellationToken)
    {
        // EndpointConvention map'ine gore update command gonderir.
        await _sendEndpointProvider.Send(command, cancellationToken);
    }

    public async Task PublishVerifyAsync(VerifyServerCommand command, CancellationToken cancellationToken)
    {
        // EndpointConvention map'ine gore verify command gonderir.
        await _sendEndpointProvider.Send(command, cancellationToken);
    }
}
