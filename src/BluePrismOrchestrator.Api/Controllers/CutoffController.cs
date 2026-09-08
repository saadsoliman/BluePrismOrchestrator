using Microsoft.AspNetCore.Mvc;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Services;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CutoffController : ControllerBase
{
    private readonly ICutoffService _cutoffService;
    private readonly ILogger<CutoffController> _logger;

    public CutoffController(ICutoffService cutoffService, ILogger<CutoffController> logger)
    {
        _cutoffService = cutoffService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CutoffPolicyDto>>> GetPolicies()
    {
        var policies = await _cutoffService.GetAllPoliciesAsync();
        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CutoffPolicyDto>> GetPolicy(Guid id)
    {
        var policy = await _cutoffService.GetPolicyAsync(id);
        if (policy == null) return NotFound();
        return Ok(policy);
    }

    [HttpPost]
    public async Task<ActionResult<CutoffPolicyDto>> CreatePolicy([FromBody] CutoffPolicyDto dto)
    {
        var result = await _cutoffService.CreatePolicyAsync(dto);
        return CreatedAtAction(nameof(GetPolicy), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CutoffPolicyDto>> UpdatePolicy(Guid id, [FromBody] CutoffPolicyDto dto)
    {
        try
        {
            var result = await _cutoffService.UpdatePolicyAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var deleted = await _cutoffService.DeletePolicyAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<CutoffPolicyDto>> TogglePolicy(Guid id, [FromQuery] bool enabled)
    {
        try
        {
            var result = await _cutoffService.TogglePolicyAsync(id, enabled);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<CutoffCheckResultDto>> EvaluateCutoffs()
    {
        var result = await _cutoffService.EvaluateCutoffsAsync();
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<CutoffEventDto>>> GetHistory([FromQuery] int limit = 50)
    {
        var history = await _cutoffService.GetCutoffHistoryAsync(limit);
        return Ok(history);
    }
}