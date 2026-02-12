namespace Worker.Configuration;

/// <summary>
/// Update sonrasi verify zincirini runtime'da acip kapatir.
/// </summary>
public sealed class VerificationOptions
{
    public bool EnablePostUpdateVerification { get; set; } = true;
}
