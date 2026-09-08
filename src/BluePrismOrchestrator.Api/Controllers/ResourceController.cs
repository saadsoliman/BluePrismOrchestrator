using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourceController : ControllerBase
{
    private readonly BluePrismDbReader _bpReader;
    private readonly OrchestratorDbContext _db;
    private readonly ILogger<ResourceController> _logger;

    public ResourceController(
        BluePrismDbReader bpReader,
        OrchestratorDbContext db,
        ILogger<ResourceController> logger)
    {
        _bpReader = bpReader;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResourceStatusDto>>> GetResources()
    {
        var resources = (await _bpReader.GetResourcesAsync()).ToList();

        var dtos = resources.Select(r => new ResourceStatusDto
        {
            Name = r.Name,
            IsOnline = r.IsOnline,
            ActiveSessions = r.ProcessesRunning ?? 0,
            UtilizationPercent = r.IsOnline ? Math.Min(100, (r.ProcessesRunning ?? 0) * 35.0 + 10) : 0,
            PoolName = r.PoolName
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{name}/utilization")]
    public async Task<ActionResult<List<TrendDataPoint>>> GetResourceUtilization(string name, [FromQuery] int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        var utilizations = await _db.ResourceUtilizations
            .Where(r => r.ResourceName == name && r.Timestamp >= cutoff)
            .OrderBy(r => r.Timestamp)
            .Select(r => new TrendDataPoint
            {
                Timestamp = r.Timestamp,
                Value = r.UtilizationPercent
            })
            .ToListAsync();

        return Ok(utilizations);
    }
}
