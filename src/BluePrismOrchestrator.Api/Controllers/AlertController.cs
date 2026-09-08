using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
    private readonly OrchestratorDbContext _db;
    private readonly ILogger<AlertController> _logger;

    public AlertController(OrchestratorDbContext db, ILogger<AlertController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("rules")]
    public async Task<ActionResult<List<AlertRuleDto>>> GetRules()
    {
        var rules = await _db.AlertRules.ToListAsync();
        var dtos = rules.Select(r => new AlertRuleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            RuleType = r.RuleType,
            Severity = r.Severity,
            ProcessName = r.ProcessName,
            ResourceName = r.ResourceName,
            ThresholdValue = r.ThresholdValue,
            Operator = r.Operator,
            MetricName = r.MetricName,
            TimeWindowMinutes = r.TimeWindowMinutes,
            CooldownMinutes = r.CooldownMinutes,
            Channels = r.Channels,
            EmailRecipients = r.EmailRecipients,
            IsEnabled = r.IsEnabled
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("rules")]
    public async Task<ActionResult<AlertRuleDto>> CreateRule([FromBody] AlertRuleDto dto)
    {
        var rule = new AlertRule
        {
            Name = dto.Name,
            Description = dto.Description,
            RuleType = dto.RuleType,
            Severity = dto.Severity,
            ProcessName = dto.ProcessName,
            ResourceName = dto.ResourceName,
            ThresholdValue = dto.ThresholdValue,
            Operator = dto.Operator,
            MetricName = dto.MetricName,
            TimeWindowMinutes = dto.TimeWindowMinutes,
            CooldownMinutes = dto.CooldownMinutes,
            Channels = dto.Channels,
            EmailRecipients = dto.EmailRecipients ?? new(),
            IsEnabled = dto.IsEnabled
        };

        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();

        dto.Id = rule.Id;
        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, dto);
    }

    [HttpPut("rules/{id}")]
    public async Task<ActionResult<AlertRuleDto>> UpdateRule(Guid id, [FromBody] AlertRuleDto dto)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.Name = dto.Name;
        rule.Description = dto.Description;
        rule.RuleType = dto.RuleType;
        rule.Severity = dto.Severity;
        rule.ProcessName = dto.ProcessName;
        rule.ResourceName = dto.ResourceName;
        rule.ThresholdValue = dto.ThresholdValue;
        rule.Operator = dto.Operator;
        rule.MetricName = dto.MetricName;
        rule.TimeWindowMinutes = dto.TimeWindowMinutes;
        rule.CooldownMinutes = dto.CooldownMinutes;
        rule.Channels = dto.Channels;
        rule.EmailRecipients = dto.EmailRecipients ?? new();
        rule.IsEnabled = dto.IsEnabled;

        await _db.SaveChangesAsync();
        dto.Id = rule.Id;
        return Ok(dto);
    }

    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<AlertHistoryDto>>> GetHistory([FromQuery] int limit = 50)
    {
        var alerts = await _db.AlertHistory
            .OrderByDescending(a => a.FiredAt)
            .Take(limit)
            .Select(a => new AlertHistoryDto
            {
                Id = a.Id,
                RuleName = a.RuleName,
                Severity = a.Severity,
                Message = a.Message,
                TriggerValue = a.TriggerValue,
                ProcessName = a.ProcessName,
                ResourceName = a.ResourceName,
                IsAcknowledged = a.IsAcknowledged,
                FiredAt = a.FiredAt
            })
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPost("history/{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    {
        var alert = await _db.AlertHistory.FindAsync(id);
        if (alert == null) return NotFound();

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = "User";
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
