using Infrastructure.Messaging;

namespace Infrastructure.Tests;

public sealed class DeterministicMessageIdFactoryTests
{
    [Fact]
    public void ForUpdate_SameInput_ProducesSameGuid()
    {
        var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var first = DeterministicMessageIdFactory.ForUpdate(jobId, targetId);
        var second = DeterministicMessageIdFactory.ForUpdate(jobId, targetId);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ForUpdate_DifferentTarget_ProducesDifferentGuid()
    {
        var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetA = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var targetB = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var first = DeterministicMessageIdFactory.ForUpdate(jobId, targetA);
        var second = DeterministicMessageIdFactory.ForUpdate(jobId, targetB);

        Assert.NotEqual(first, second);
    }
}
