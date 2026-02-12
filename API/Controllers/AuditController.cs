using API.Contracts.Audit;
using Application.Abstractions.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("audit")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditTrailReader _auditTrailReader;

    public AuditController(IAuditTrailReader auditTrailReader)
    {
        _auditTrailReader = auditTrailReader;
    }

    /// <summary>
    /// Son audit kayitlarini listeler (raporlama gorunumu).
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogResponse>> GetRecent(
        [FromQuery] int take = 100,
        [FromQuery] string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _auditTrailReader.GetRecentAsync(take, correlationId, cancellationToken);
        var response = new AuditLogResponse
        {
            Items = items.Select(item => new AuditLogItem
            {
                Id = item.Id,
                Who = item.Who,
                When = item.When,
                TicketRef = item.TicketRef,
                TargetAccount = item.TargetAccount,
                ServerGroup = item.ServerGroup,
                ResultSummary = item.ResultSummary,
                CorrelationId = item.CorrelationId
            }).ToList()
        };

        return Ok(response);
    }
}
