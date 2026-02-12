using System.Text.RegularExpressions;

namespace Infrastructure.Security;

/// <summary>
/// Audit/log metinlerinde hassas ifadeleri maskeleyen yardimci.
/// </summary>
internal static partial class SensitiveDataRedactor
{
    private const int MaxLength = 1000;

    /// <summary>
    /// Hassas anahtar-deger desenlerini REDACTED ile degistirir.
    /// </summary>
    public static string? Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = value.Replace(Environment.NewLine, " ", StringComparison.Ordinal);
        var redacted = SensitivePattern().Replace(normalized, "$1=[REDACTED]");

        if (redacted.Length > MaxLength)
        {
            return redacted[..MaxLength];
        }

        return redacted;
    }

    [GeneratedRegex("(?i)(oldpassword|newpassword|password|token|secret|apikey|authorization)\\s*[:=]\\s*([^\\s,;]+)")]
    private static partial Regex SensitivePattern();
}
