using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBQueryAIEngine.Api.Controllers;

[ApiController]
[Route("api/sql-templates")]
public sealed class TemplatesController(ISqlTemplateService templateService) : ControllerBase
{
    /// <summary>
    /// Generates reusable SQL templates for daily, weekly, monthly, and quarterly analytics over the configured warehouse tables.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SqlTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SqlTemplate>>> GetAsync(CancellationToken cancellationToken)
    {
        var templates = await templateService.GetTemplatesAsync(cancellationToken);
        return Ok(templates);
    }
}
