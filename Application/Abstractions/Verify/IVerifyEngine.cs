namespace Application.Abstractions.Verify;

/// <summary>
/// Target uzerindeki update sonucunu kaynak bazli dogrular.
/// </summary>
public interface IVerifyEngine
{
    Task<VerifyResult> VerifyAsync(VerifyContext context, CancellationToken cancellationToken);
}
