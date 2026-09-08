using Microsoft.AspNetCore.Mvc;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Models.BluePrism;
using BluePrismOrchestrator.Api.Services;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly BluePrismDbReader _bpReader;
    private readonly IAutomateCService _automateC;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        BluePrismDbReader bpReader,
        IAutomateCService automateC,
        ILogger<SessionController> logger)
    {
        _bpReader = bpReader;
        _automateC = automateC;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetSessions([FromQuery] bool activeOnly = true)
    {
        var now = DateTime.UtcNow;
        var sessions = activeOnly
            ? (await _bpReader.GetActiveSessionsAsync()).ToList()
            : (await _bpReader.GetRecentSessionsAsync(50)).ToList();

        var dtos = sessions.Select(s => new SessionDto
        {
            SessionId = s.SessionId,
            ProcessName = s.ProcessName ?? "Unknown",
            ResourceName = s.ResourceName ?? "Unknown",
            Status = s.StatusDescription ?? BPStatusMap.GetDescription(s.StatusId),
            StartTime = s.StartDateTime,
            EndTime = s.EndDateTime,
            DurationSeconds = s.StartDateTime.HasValue
                ? (s.EndDateTime ?? now).Subtract(s.StartDateTime.Value).TotalSeconds
                : null
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> StopSession(Guid id, [FromQuery] string processName, [FromQuery] string resourceName)
    {
        _logger.LogInformation("Requesting stop for session {SessionId} on {Resource}", id, resourceName);
        var result = await _automateC.RequestStopAsync(processName, resourceName);

        return Ok(new
        {
            success = result.Success,
            message = result.Success ? "Stop request sent to Blue Prism runtime" : result.Error
        });
    }
}
