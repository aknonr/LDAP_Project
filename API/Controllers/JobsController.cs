using API.Contracts.Jobs;
using API.Logging;
using Application.Abstractions.Auditing;
using Application.UseCases.Jobs;
using Application.UseCases.Jobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("jobs")]
[Authorize]
public sealed class JobsController : ControllerBase
{
    private const string DiscoveryOperation = "DISCOVERY_JOB_CREATE";
    private const string PasswordChangeOperation = "PASSWORD_CHANGE_JOB_CREATE";

    private readonly CreateDiscoveryJobUseCase _createDiscoveryJobUseCase;
    private readonly CreatePasswordChangeJobUseCase _createPasswordChangeJobUseCase;
    private readonly GetJobStatusUseCase _getJobStatusUseCase;
    private readonly GetJobTargetsUseCase _getJobTargetsUseCase;
    private readonly IAuditTrailWriter _auditTrailWriter;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        CreateDiscoveryJobUseCase createDiscoveryJobUseCase,
        CreatePasswordChangeJobUseCase createPasswordChangeJobUseCase,
        GetJobStatusUseCase getJobStatusUseCase,
        GetJobTargetsUseCase getJobTargetsUseCase,
        IAuditTrailWriter auditTrailWriter,
        ILogger<JobsController> logger)
    {
        _createDiscoveryJobUseCase = createDiscoveryJobUseCase;
        _createPasswordChangeJobUseCase = createPasswordChangeJobUseCase;
        _getJobStatusUseCase = getJobStatusUseCase;
        _getJobTargetsUseCase = getJobTargetsUseCase;
        _auditTrailWriter = auditTrailWriter;
        _logger = logger;
    }

    /// <summary>
    /// Discovery job olusturur ve hedefler icin discovery komutu publish eder.
    /// </summary>
    [HttpPost("discovery")]
    [Authorize(Roles = "Admin,Operator")]
    [ProducesResponseType(typeof(JobCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobCreatedResponse>> CreateDiscoveryJob(
        [FromBody] CreateDiscoveryJobRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId();
        var requestedBy = ResolveRequestedBy();

        var input = new CreateDiscoveryJobInput
        {
            ServerGroupExternalId = request.ServerGroupId,
            RequestedBy = requestedBy,
            TicketRef = request.TicketRef,
            CorrelationId = correlationId
        };

        var result = await _createDiscoveryJobUseCase.ExecuteAsync(input, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                "n/a",
                request.ServerGroupId,
                $"{DiscoveryOperation}:FAILED:{result.ErrorCode ?? "UNKNOWN"}",
                correlationId,
                cancellationToken);
            return MapCreateError(result.ErrorCode, result.ErrorMessage);
        }

        await WriteAuditSafelyAsync(
            requestedBy,
            request.TicketRef,
            "n/a",
            request.ServerGroupId,
            $"{DiscoveryOperation}:SUCCESS:JobId={result.Value.JobId}",
            correlationId,
            cancellationToken);

        var response = new JobCreatedResponse
        {
            JobId = result.Value.JobId,
            Status = result.Value.Status,
            CreatedAt = result.Value.CreatedAt
        };

        return CreatedAtAction(nameof(GetJobStatus), new { id = response.JobId }, response);
    }

    /// <summary>
    /// Password change job olusturur ve hedefler icin update komutu publish eder.
    /// </summary>
    [HttpPost("password-change")]
    [Authorize(Roles = "Admin,Operator")]
    [ProducesResponseType(typeof(JobCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobCreatedResponse>> CreatePasswordChangeJob(
        [FromBody] CreatePasswordChangeJobRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId();
        var requestedBy = ResolveRequestedBy();

        if (request.OldPassword == request.NewPassword)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                request.TargetAccount,
                request.ServerGroupId,
                $"{PasswordChangeOperation}:FAILED:SAME_PASSWORD",
                correlationId,
                cancellationToken);
            return BadRequest("Old ve new password ayni olamaz.");
        }

        var input = new CreatePasswordChangeJobInput
        {
            ServerGroupExternalId = request.ServerGroupId,
            RequestedBy = requestedBy,
            TargetAccount = request.TargetAccount,
            OldPassword = request.OldPassword,
            NewPassword = request.NewPassword,
            TicketRef = request.TicketRef,
            CorrelationId = correlationId
        };

        var result = await _createPasswordChangeJobUseCase.ExecuteAsync(input, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            await WriteAuditSafelyAsync(
                requestedBy,
                request.TicketRef,
                request.TargetAccount,
                request.ServerGroupId,
                $"{PasswordChangeOperation}:FAILED:{result.ErrorCode ?? "UNKNOWN"}",
                correlationId,
                cancellationToken);
            return MapCreateError(result.ErrorCode, result.ErrorMessage);
        }

        await WriteAuditSafelyAsync(
            requestedBy,
            request.TicketRef,
            request.TargetAccount,
            request.ServerGroupId,
            $"{PasswordChangeOperation}:SUCCESS:JobId={result.Value.JobId}",
            correlationId,
            cancellationToken);

        var response = new JobCreatedResponse
        {
            JobId = result.Value.JobId,
            Status = result.Value.Status,
            CreatedAt = result.Value.CreatedAt
        };

        return CreatedAtAction(nameof(GetJobStatus), new { id = response.JobId }, response);
    }

    /// <summary>
    /// Job durumunu ve temel ozet metrikleri getirir.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Operator,Viewer")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getJobStatusUseCase.ExecuteAsync(new GetJobStatusInput { JobId = id }, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound(result.ErrorMessage ?? "Job bulunamadi.");
        }

        return Ok(new JobStatusResponse
        {
            JobId = result.Value.JobId,
            Type = result.Value.Type,
            Status = result.Value.Status,
            CreatedAt = result.Value.CreatedAt,
            CompletedAt = result.Value.CompletedAt,
            TargetCount = result.Value.TargetCount,
            SuccessCount = result.Value.SuccessCount,
            FailedCount = result.Value.FailedCount
        });
    }

    /// <summary>
    /// Job hedef listesini sayfali olarak getirir.
    /// </summary>
    [HttpGet("{id:guid}/targets")]
    [Authorize(Roles = "Admin,Operator,Viewer")]
    [ProducesResponseType(typeof(JobTargetsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobTargetsResponse>> GetJobTargets(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        var result = await _getJobTargetsUseCase.ExecuteAsync(
            new GetJobTargetsInput
            {
                JobId = id,
                Skip = skip,
                Take = take
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorCode, "JOB_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result.ErrorMessage ?? "Job bulunamadi.");
            }

            return BadRequest(result.ErrorMessage ?? "Gecersiz hedef sorgusu.");
        }

        if (result.Value is null)
        {
            return NotFound("Job bulunamadi.");
        }

        var response = new JobTargetsResponse
        {
            JobId = result.Value.JobId,
            TotalCount = result.Value.TotalCount,
            Targets = result.Value.Targets.Select(target => new JobTargetDto
            {
                TargetId = target.TargetId,
                ServerName = target.ServerName,
                Status = target.Status,
                ErrorCode = target.ErrorCode,
                ErrorMessage = target.ErrorMessage,
                UpdatedAt = target.UpdatedAt
            }).ToList()
        };

        return Ok(response);
    }

    private ActionResult<JobCreatedResponse> MapCreateError(string? errorCode, string? errorMessage)
    {
        // Use-case hata kodunu HTTP sonucuna map eder.
        if (string.Equals(errorCode, "SERVER_GROUP_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(errorMessage ?? "Server group bulunamadi.");
        }

        if (string.Equals(errorCode, "SERVER_GROUP_EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(errorMessage ?? "Server group bos.");
        }

        if (string.Equals(errorCode, "PAYLOAD_PROTECTION_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Payload sifreleme altyapisi hazir degil.");
        }

        return BadRequest(errorMessage ?? "Job olusturma basarisiz.");
    }

    private string ResolveRequestedBy()
    {
        // Islem yapan kullaniciyi claim'lerden cozer.
        return User.FindFirstValue("preferred_username")
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? "unknown";
    }

    private string? ResolveCorrelationId()
    {
        // Request pipeline standardina gore correlation degeri cozer.
        return CorrelationIdAccessor.Get(HttpContext);
    }

    private async Task WriteAuditSafelyAsync(
        string who,
        string? ticketRef,
        string targetAccount,
        string serverGroup,
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
                    ServerGroup = serverGroup,
                    ResultSummary = resultSummary,
                    CorrelationId = correlationId
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit kaydi yazilamasa da asil is akisinin sonucunu bozmadan raporlar.
            _logger.LogError(ex, "Audit kaydi yazilamadi. CorrelationId={CorrelationId}", correlationId);
        }
    }
}
