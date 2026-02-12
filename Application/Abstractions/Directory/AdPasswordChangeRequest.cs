namespace Application.Abstractions.Directory;

/// <summary>
/// AD password change istegi (old + new).
/// </summary>
public sealed record AdPasswordChangeRequest
{
    public string UserDnOrUpn { get; init; } = string.Empty;
    public string OldPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
