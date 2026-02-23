using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.Discovery.Strategies;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests;

public sealed class UserRightDiscoveryStrategyTests
{
    [Fact]
    public async Task DiscoverAsync_ParsesArrayPayload()
    {
        var json = "[{\"RightName\":\"SeServiceLogonRight\",\"Account\":\"CONTOSO\\\\svc-1\",\"Sid\":\"S-1-5-21-1\"}]";
        var executor = new FakeRemoteExecutor(json);
        var strategy = new UserRightDiscoveryStrategy(executor, NullLogger<UserRightDiscoveryStrategy>.Instance);

        var result = await strategy.DiscoverAsync(
            new DiscoveryContext
            {
                JobId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                ServerName = "server-1"
            },
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(ResourceType.UserRight, result[0].ResourceType);
        Assert.Equal("SeServiceLogonRight", result[0].ResourceName);
        Assert.Equal(@"CONTOSO\svc-1", result[0].ResourcePath);
    }

    [Fact]
    public async Task DiscoverAsync_ParsesSingleObjectPayload()
    {
        var json = "{\"RightName\":\"SeBatchLogonRight\",\"Account\":\"CONTOSO\\\\svc-2\",\"Sid\":\"S-1-5-21-2\"}";
        var executor = new FakeRemoteExecutor(json);
        var strategy = new UserRightDiscoveryStrategy(executor, NullLogger<UserRightDiscoveryStrategy>.Instance);

        var result = await strategy.DiscoverAsync(
            new DiscoveryContext
            {
                JobId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                ServerName = "server-1"
            },
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(ResourceType.UserRight, result[0].ResourceType);
        Assert.Equal("SeBatchLogonRight", result[0].ResourceName);
        Assert.Equal(@"CONTOSO\svc-2", result[0].ResourcePath);
    }

    private sealed class FakeRemoteExecutor : IRemoteCommandExecutor
    {
        private readonly string _output;

        public FakeRemoteExecutor(string output)
        {
            _output = output;
        }

        public Task<RemoteCommandExecutionResult> ExecutePowerShellAsync(
            string serverName,
            string scriptBlock,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(RemoteCommandExecutionResult.Success(_output));
        }
    }
}
