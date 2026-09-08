using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Services;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessOutputController : ControllerBase
{
    private readonly IBusinessOutputService _outputService;
    private readonly ILogger<BusinessOutputController> _logger;

    public BusinessOutputController(IBusinessOutputService outputService, ILogger<BusinessOutputController> logger)
    {
        _outputService = outputService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<BusinessOutputRuleDto>>> GetRules()
    {
        var rules = await _outputService.GetAllRulesAsync();
        return Ok(rules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BusinessOutputRuleDto>> GetRule(Guid id)
    {
        var rule = await _outputService.GetRuleAsync(id);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<BusinessOutputRuleDto>> CreateRule([FromBody] BusinessOutputRuleDto dto)
    {
        var result = await _outputService.CreateRuleAsync(dto);
        return CreatedAtAction(nameof(GetRule), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BusinessOutputRuleDto>> UpdateRule(Guid id, [FromBody] BusinessOutputRuleDto dto)
    {
        try
        {
            var result = await _outputService.UpdateRuleAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var deleted = await _outputService.DeleteRuleAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<BusinessOutputRuleDto>> ToggleRule(Guid id, [FromQuery] bool enabled)
    {
        try
        {
            var result = await _outputService.ToggleRuleAsync(id, enabled);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("deliver")]
    public async Task<ActionResult<List<BusinessOutputDeliveryLogDto>>> DeliverOutputs()
    {
        var logs = await _outputService.DeliverOutputsAsync();
        return Ok(logs);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<List<BusinessOutputDeliveryLogDto>>> GetDeliveryLogs([FromQuery] int limit = 50)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var logs = await db.BusinessOutputDeliveryLogs
            .OrderByDescending(l => l.DeliveredAt)
            .Take(limit)
            .Select(l => new BusinessOutputDeliveryLogDto
            {
                Id = l.Id,
                RuleId = l.BusinessOutputRuleId,
                ProcessName = l.ProcessName,
                FileName = l.FileName,
                DeliveredAt = l.DeliveredAt,
                Status = l.Status,
                Message = l.Message,
            })
            .ToListAsync();

        return Ok(logs);
    }
}