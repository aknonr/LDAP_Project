namespace Infrastructure.RemoteExecution;

/// <summary>
/// Uzaktan komut calistirma sonucu.
/// </summary>
public sealed record RemoteCommandExecutionResult
{
    public bool IsSuccess { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static RemoteCommandExecutionResult Success(string? output)
        => new() { IsSuccess = true, StandardOutput = output };

    public static RemoteCommandExecutionResult Failure(string errorCode, string errorMessage, string? standardError = null)
        => new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            StandardError = standardError
        };
}
