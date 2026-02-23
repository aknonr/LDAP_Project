using API.Contracts.Admin;
using Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("admin/roles")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminRolesController : ControllerBase
{
    private readonly ListRolesUseCase _listRolesUseCase;

    public AdminRolesController(ListRolesUseCase listRolesUseCase)
    {
        _listRolesUseCase = listRolesUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RoleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleListResponse>> ListRoles(CancellationToken cancellationToken)
    {
        var result = await _listRolesUseCase.ExecuteAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Rol listesi okunamadi.");
        }

        return Ok(new RoleListResponse
        {
            Roles = result.Value.Roles
        });
    }
}

