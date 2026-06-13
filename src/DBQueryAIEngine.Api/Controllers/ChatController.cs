using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBQueryAIEngine.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatOrchestrator orchestrator) : ControllerBase
{
    /// <summary>
    /// Accepts a business analytics question, generates a guarded SQL query, executes Fabric DW, and returns insight text plus chart-ready data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> AskAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var response = await orchestrator.AnswerAsync(request, cancellationToken);
        return Ok(response);
    }
}
