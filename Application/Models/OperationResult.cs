namespace Application.Models;

/// <summary>
/// Altyapi servisleri icin basit basari/hata kapsayicisi.
/// </summary>
public sealed record OperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult Success()
        => new() { IsSuccess = true };

    public static OperationResult Failure(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
