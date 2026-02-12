namespace Application.Models;

/// <summary>
/// Is katmanlari arasinda standart hata kodu tasimak icin kullanilir.
/// </summary>
public sealed class OperationFailureException : Exception
{
    public OperationFailureException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
