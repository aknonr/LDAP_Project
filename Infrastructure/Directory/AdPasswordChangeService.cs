using Application.Abstractions.Directory;
using Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Infrastructure.Directory;

public sealed class AdPasswordChangeService : IAdPasswordChangeService
{
    private const string AttributeName = "unicodePwd";
    private readonly IOptionsMonitor<LdapOptions> _options;
    private readonly ILogger<AdPasswordChangeService> _logger;

    public AdPasswordChangeService(
        IOptionsMonitor<LdapOptions> options,
        ILogger<AdPasswordChangeService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<OperationResult> ChangePasswordAsync(AdPasswordChangeRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.UserDnOrUpn)
            || string.IsNullOrWhiteSpace(request.OldPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Task.FromResult(OperationResult.Failure("UNKNOWN", "Gerekli alanlar bos olamaz."));
        }

        if (string.Equals(request.OldPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return Task.FromResult(OperationResult.Failure("POLICY_VIOLATION", "Old ve new password ayni olamaz."));
        }

        var options = _options.CurrentValue;
        if (!options.UseSsl || options.Port != 636)
        {
            return Task.FromResult(OperationResult.Failure("LDAPS_CONNECT_FAILED", "LDAPS 636 zorunludur."));
        }

        try
        {
            using var connection = CreateConnection(options, request.UserDnOrUpn, request.OldPassword);
            connection.Bind();

            var deleteOldPassword = new DirectoryAttributeModification
            {
                Name = AttributeName,
                Operation = DirectoryAttributeOperation.Delete
            };
            deleteOldPassword.Add(EncodePassword(request.OldPassword));

            var addNewPassword = new DirectoryAttributeModification
            {
                Name = AttributeName,
                Operation = DirectoryAttributeOperation.Add
            };
            addNewPassword.Add(EncodePassword(request.NewPassword));

            var modifyRequest = new ModifyRequest(
                request.UserDnOrUpn,
                deleteOldPassword,
                addNewPassword);

            var response = (ModifyResponse)connection.SendRequest(modifyRequest);
            if (response.ResultCode == ResultCode.Success)
            {
                return Task.FromResult(OperationResult.Success());
            }

            return Task.FromResult(MapResultCode(response.ResultCode, response.ErrorMessage));
        }
        catch (DirectoryOperationException ex)
        {
            return Task.FromResult(MapResultCode(ex.Response?.ResultCode, ex.Message));
        }
        catch (LdapException ex)
        {
            return Task.FromResult(MapLdapException(ex));
        }
        catch (TimeoutException ex)
        {
            return Task.FromResult(OperationResult.Failure("TIMEOUT", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LDAPS password change beklenmeyen hata.");
            return Task.FromResult(OperationResult.Failure("UNKNOWN", "Beklenmeyen hata."));
        }
    }

    private static LdapConnection CreateConnection(LdapOptions options, string userDnOrUpn, string password)
    {
        var identifier = new LdapDirectoryIdentifier(options.Host, options.Port, true, false);
        var connection = new LdapConnection(identifier)
        {
            AuthType = ResolveAuthType(options.AuthType),
            Credential = new NetworkCredential(userDnOrUpn, password),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 10 : options.TimeoutSeconds)
        };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = true;

        if (!options.ValidateServerCertificate)
        {
            connection.SessionOptions.VerifyServerCertificate = (_, _) => true;
        }

        return connection;
    }

    private static AuthType ResolveAuthType(string? authType)
    {
        if (string.Equals(authType, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return AuthType.Basic;
        }

        if (string.Equals(authType, "Ntlm", StringComparison.OrdinalIgnoreCase))
        {
            return AuthType.Ntlm;
        }

        return AuthType.Negotiate;
    }

    private static byte[] EncodePassword(string password)
    {
        return Encoding.Unicode.GetBytes($"\"{password}\"");
    }

    private static OperationResult MapResultCode(ResultCode? resultCode, string? message)
    {
        if (resultCode is null)
        {
            return OperationResult.Failure("UNKNOWN", message ?? "LDAP sonucu alinamadi.");
        }

        return resultCode switch
        {
            ResultCode.ConstraintViolation => OperationResult.Failure("POLICY_VIOLATION", message ?? "Password policy ihlali."),
            ResultCode.InsufficientAccessRights => OperationResult.Failure("ACCESS_DENIED", message ?? "Erisim reddedildi."),
            ResultCode.NoSuchObject => OperationResult.Failure("USER_NOT_FOUND", message ?? "Kullanici bulunamadi."),
            ResultCode.TimeLimitExceeded => OperationResult.Failure("TIMEOUT", message ?? "LDAP timeout."),
            ResultCode.Unavailable => OperationResult.Failure("LDAPS_CONNECT_FAILED", message ?? "LDAP server baglantisi basarisiz."),
            ResultCode.InappropriateAuthentication => OperationResult.Failure("INVALID_CREDENTIALS", message ?? "Invalid credentials."),
            _ => OperationResult.Failure("UNKNOWN", message ?? $"LDAP hata kodu: {resultCode}")
        };
    }

    private static OperationResult MapLdapException(LdapException ex)
    {
        return ex.ErrorCode switch
        {
            49 => OperationResult.Failure("INVALID_CREDENTIALS", ex.Message),
            19 => OperationResult.Failure("POLICY_VIOLATION", ex.Message),
            50 => OperationResult.Failure("ACCESS_DENIED", ex.Message),
            32 => OperationResult.Failure("USER_NOT_FOUND", ex.Message),
            81 => OperationResult.Failure("LDAPS_CONNECT_FAILED", ex.Message),
            85 => OperationResult.Failure("TIMEOUT", ex.Message),
            _ => OperationResult.Failure("UNKNOWN", ex.Message)
        };
    }
}
