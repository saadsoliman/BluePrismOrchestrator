using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly OrchestratorDbContext _db;
    private readonly BluePrismDbReader _bpReader;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        OrchestratorDbContext db,
        BluePrismDbReader bpReader,
        ILogger<AnalyticsController> logger)
    {
        _db = db;
        _bpReader = bpReader;
        _logger = logger;
    }

    [HttpGet("trends")]
    public async Task<ActionResult<AnalyticsTrendDto>> GetTrends(
        [FromQuery] string? processName = null,
        [FromQuery] int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var targetProcess = processName ?? "Invoice_Processing";

        var executions = await _db.Executions
            .Where(e => e.ProcessName == targetProcess && e.StartedAt >= cutoff)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();

        var durationTrend = new List<TrendDataPoint>();
        var successRateTrend = new List<TrendDataPoint>();
        var throughputTrend = new List<TrendDataPoint>();

        // Group by day
        for (int i = days - 1; i >= 0; i--)
        {
            var dayStart = DateTime.UtcNow.Date.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);

            var dayExecs = executions.Where(e => e.StartedAt >= dayStart && e.StartedAt < dayEnd).ToList();
            var count = dayExecs.Count;
            var success = dayExecs.Count(e => e.Status == ExecutionStatus.Completed);

            durationTrend.Add(new TrendDataPoint
            {
                Timestamp = dayStart,
                Value = dayExecs.Any(e => e.DurationSeconds.HasValue)
                    ? Math.Round(dayExecs.Where(e => e.DurationSeconds.HasValue).Average(e => e.DurationSeconds!.Value), 1)
                    : null
            });

            successRateTrend.Add(new TrendDataPoint
            {
                Timestamp = dayStart,
                Value = count > 0 ? Math.Round((double)success / count * 100, 1) : null
            });

            throughputTrend.Add(new TrendDataPoint
            {
                Timestamp = dayStart,
                Value = count > 0 ? count : null
            });
        }

        return Ok(new AnalyticsTrendDto
        {
            ProcessName = targetProcess,
            DurationTrend = durationTrend,
            SuccessRateTrend = successRateTrend,
            ThroughputTrend = throughputTrend
        });
    }

    [HttpGet("sla-compliance")]
    public async Task<ActionResult<SlaComplianceDto>> GetSlaCompliance([FromQuery] int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var executions = await _db.Executions
            .Where(e => e.StartedAt >= cutoff && e.SlaDeadline.HasValue)
            .ToListAsync();

        var total = executions.Count;
        var breaches = executions.Count(e => e.SlaBreached);
        var compliance = total > 0 ? Math.Round((double)(total - breaches) / total * 100, 1) : 0;

        var complianceTrend = new List<TrendDataPoint>();
        for (int i = days - 1; i >= 0; i--)
        {
            var d = DateTime.UtcNow.Date.AddDays(-i);
            var dayExecs = executions.Where(e => e.StartedAt.HasValue && e.StartedAt!.Value.Date == d).ToList();
            var dayBreach = dayExecs.Count(e => e.SlaBreached);
            var dayTotal = dayExecs.Count;
            complianceTrend.Add(new TrendDataPoint
            {
                Timestamp = d,
                Value = dayTotal > 0 ? Math.Round((double)(dayTotal - dayBreach) / dayTotal * 100, 1) : (double?)null ?? 100
            });
        }

        // Derive worst-performing processes from real execution data
        var worstProcesses = executions
            .GroupBy(e => e.ProcessName)
            .Select(g => new SlaProcessDetail
            {
                ProcessName = g.Key,
                CompliancePercent = g.Count() > 0
                    ? Math.Round((double)(g.Count(e => !e.SlaBreached) / (double)g.Count()) * 100, 1)
                    : 0,
                Breaches = g.Count(e => e.SlaBreached)
            })
            .Where(p => p.Breaches > 0)
            .OrderBy(p => p.CompliancePercent)
            .Take(5)
            .ToList();

        if (!worstProcesses.Any())
        {
            worstProcesses.Add(new SlaProcessDetail
            {
                ProcessName = "No SLA breaches recorded",
                CompliancePercent = 100,
                Breaches = 0
            });
        }

        return Ok(new SlaComplianceDto
        {
            OverallCompliancePercent = compliance,
            TotalExecutions = total,
            SlaBreaches = breaches,
            ComplianceTrend = complianceTrend,
            WorstProcesses = worstProcesses
        });
    }

    [HttpGet("resource-heatmap")]
    public async Task<ActionResult<List<ResourceHeatmapDto>>> GetResourceHeatmap()
    {
        var resources = (await _bpReader.GetResourcesAsync()).Take(6).ToList();
        var result = new List<ResourceHeatmapDto>();

        foreach (var r in resources)
        {
            var cells = new List<HeatmapCell>();
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour += 3)
                {
                    // Enterprise work pattern simulation
                    double baseUtil = (hour >= 9 && hour <= 18 && day >= 1 && day <= 5) ? 75.0 : 20.0;
                    cells.Add(new HeatmapCell
                    {
                        DayOfWeek = day,
                        Hour = hour,
                        UtilizationPercent = Math.Clamp(baseUtil + ((hour * 7 + day * 13) % 25), 5, 95)
                    });
                }
            }

            result.Add(new ResourceHeatmapDto
            {
                ResourceName = r.Name,
                Cells = cells
            });
        }

        return Ok(result);
    }
}
