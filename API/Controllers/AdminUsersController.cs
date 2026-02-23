using API.Contracts.Admin;
using API.Logging;
using Application.Abstractions.Auditing;
using Application.Services.Rbac;
using Application.UseCases.Admin;
using Application.UseCases.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminUsersController : ControllerBase
{
    private const string UserUpsertOperation = "ADMIN_USER_UPSERT";
    private const string UserActiveOperation = "ADMIN_USER_SET_ACTIVE";
    private const string UserRolesOperation = "ADMIN_USER_SET_ROLES";

    private readonly ListUsersUseCase _listUsersUseCase;
    private readonly UpsertUserUseCase _upsertUserUseCase;
    private readonly SetUserActiveUseCase _setUserActiveUseCase;
    private readonly SetUserRolesUseCase _setUserRolesUseCase;
    private readonly IAuditTrailWriter _auditTrailWriter;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        ListUsersUseCase listUsersUseCase,
        UpsertUserUseCase upsertUserUseCase,
        SetUserActiveUseCase setUserActiveUseCase,
        SetUserRolesUseCase setUserRolesUseCase,
        IAuditTrailWriter auditTrailWriter,
        ILogger<AdminUsersController> logger)
    {
        _listUsersUseCase = listUsersUseCase;
        _upsertUserUseCase = upsertUserUseCase;
        _setUserActiveUseCase = setUserActiveUseCase;
        _setUserRolesUseCase = setUserRolesUseCase;
        _auditTrailWriter = auditTrailWriter;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListUsersResponse>> ListUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _listUsersUseCase.ExecuteAsync(
            new ListUsersInput
            {
                Skip = skip,
                Take = take
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(result.ErrorMessage ?? "Gecersiz kullanici listeleme istegi.");
        }

        return Ok(new ListUsersResponse
        {
            TotalCount = result.Value.TotalCount,
            Items = result.Value.Items.Select(user => new UserDto
            {
                UserId = user.UserId,
                Subject = user.Subject,
                DisplayName = user.DisplayName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.Roles
            }).ToList()
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(UpsertUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpsertUserResponse>> UpsertUser(
        [FromBody] UpsertUserRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId();
        var requestedBy = ResolveRequestedBy();

        var result = await _upsertUserUseCase.ExecuteAsync(
            new UpsertUserInput
            {
                Subject = request.Subject,
                DisplayName = request.DisplayName,
                Email = request.Email
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                request.Subject,
                $"{UserUpsertOperation}:FAILED:{result.ErrorCode ?? "UNKNOWN"}",
                correlationId,
                cancellationToken);

            return BadRequest(result.ErrorMessage ?? "Kullanici kaydi basarisiz.");
        }

        await WriteAuditSafelyAsync(
            requestedBy,
            request.TicketRef,
            request.Subject,
            $"{UserUpsertOperation}:SUCCESS:UserId={result.Value.UserId:D}:Created={result.Value.IsCreated}",
            correlationId,
            cancellationToken);

        return Ok(new UpsertUserResponse
        {
            UserId = result.Value.UserId,
            IsCreated = result.Value.IsCreated
        });
    }

    [HttpPut("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetUserActive(
        Guid id,
        [FromBody] SetUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId();
        var requestedBy = ResolveRequestedBy();

        var result = await _setUserActiveUseCase.ExecuteAsync(
            new SetUserActiveInput
            {
                UserId = id,
                IsActive = request.IsActive
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                id.ToString("D"),
                $"{UserActiveOperation}:FAILED:{result.ErrorCode ?? "UNKNOWN"}",
                correlationId,
                cancellationToken);

            if (string.Equals(result.ErrorCode, "USER_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result.ErrorMessage ?? "Kullanici bulunamadi.");
            }

            if (string.Equals(result.ErrorCode, "LAST_ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(result.ErrorMessage ?? "Son admin pasif edilemez.");
            }

            return BadRequest(result.ErrorMessage ?? "Kullanici aktif/pasif islem basarisiz.");
        }

        await WriteAuditSafelyAsync(
            requestedBy,
            request.TicketRef,
            result.Value.Subject,
            $"{UserActiveOperation}:SUCCESS:UserId={result.Value.UserId:D}:IsActive={result.Value.IsActive}",
            correlationId,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetUserRoles(
        Guid id,
        [FromBody] SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roles.Any(role => string.Equals(role?.Trim(), KnownRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            && !User.IsInRole(KnownRoles.SuperAdmin))
        {
            return Forbid();
        }

        var correlationId = ResolveCorrelationId();
        var requestedBy = ResolveRequestedBy();

        var result = await _setUserRolesUseCase.ExecuteAsync(
            new SetUserRolesInput
            {
                UserId = id,
                Roles = request.Roles
            },
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                id.ToString("D"),
                $"{UserRolesOperation}:FAILED:{result.ErrorCode ?? "UNKNOWN"}",
                correlationId,
                cancellationToken);

            if (string.Equals(result.ErrorCode, "USER_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result.ErrorMessage ?? "Kullanici bulunamadi.");
            }

            if (string.Equals(result.ErrorCode, "LAST_ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(result.ErrorMessage ?? "Son admin'in yetkisi kaldirilamaz.");
            }

            if (string.Equals(result.ErrorCode, "ROLE_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result.ErrorMessage ?? "Rol tablosu eksik.");
            }

            return BadRequest(result.ErrorMessage ?? "Kullanici rol atama basarisiz.");
        }

        await WriteAuditSafelyAsync(
            requestedBy,
            request.TicketRef,
            result.Value.Subject,
            $"{UserRolesOperation}:SUCCESS:UserId={result.Value.UserId:D}:Roles={string.Join(",", result.Value.Roles)}",
            correlationId,
            cancellationToken);

        return NoContent();
    }

    private string ResolveRequestedBy()
    {
        return User.FindFirstValue("preferred_username")
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? "unknown";
    }

    private string? ResolveCorrelationId()
    {
        return CorrelationIdAccessor.Get(HttpContext);
    }

    private async Task WriteAuditSafelyAsync(
        string who,
        string? ticketRef,
        string targetAccount,
        string resultSummary,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditTrailWriter.WriteAsync(
                new AuditEntryWriteModel
                {
                    Who = who,
                    TicketRef = ticketRef,
                    TargetAccount = targetAccount,
                    ServerGroup = "n/a",
                    ResultSummary = resultSummary,
                    CorrelationId = correlationId
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit kaydi yazilamadi. CorrelationId={CorrelationId}", correlationId);
        }
    }
}

