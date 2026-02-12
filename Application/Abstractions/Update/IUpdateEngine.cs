namespace Application.Abstractions.Update;

/// <summary>
/// Update stratejilerini orkestre eder.
/// </summary>
public interface IUpdateEngine
{
    /// <summary>
    /// Update islemini calistirir.
    /// </summary>
    Task<UpdateResult> UpdateAsync(UpdateContext context, CancellationToken cancellationToken);
}
