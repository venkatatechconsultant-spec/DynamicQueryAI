using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBQueryAIEngine.Api.Controllers;

[ApiController]
[Route("api/schema")]
public sealed class SchemaController(IWarehouseSchemaService schemaService) : ControllerBase
{
    /// <summary>
    /// Scans the configured Fabric DW tables and returns column metadata used by the SQL template generator.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseTableSchema>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseTableSchema>>> GetAsync(CancellationToken cancellationToken)
    {
        var schema = await schemaService.GetAllowedSchemaAsync(cancellationToken);
        return Ok(schema);
    }
}
