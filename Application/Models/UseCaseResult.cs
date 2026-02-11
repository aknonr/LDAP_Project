namespace Application.Models;

/// <summary>
/// Use-case sonucu icin basit basari/hata kapsayicisi.
/// </summary>
public sealed record UseCaseResult<T>
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public T? Value { get; init; }

    /// <summary>
    /// Basarili sonucu dondurur.
    /// </summary>
    public static UseCaseResult<T> Success(T value)
        => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Hata sonucunu dondurur.
    /// </summary>
    public static UseCaseResult<T> Failure(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
