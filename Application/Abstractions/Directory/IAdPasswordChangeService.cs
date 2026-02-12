using Application.Models;

namespace Application.Abstractions.Directory;

/// <summary>
/// AD password change servis sozlesmesi.
/// </summary>
public interface IAdPasswordChangeService
{
    /// <summary>
    /// Old+New password ile degistirme yapar (reset yok).
    /// </summary>
    Task<OperationResult> ChangePasswordAsync(AdPasswordChangeRequest request, CancellationToken cancellationToken);
}
