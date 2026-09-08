using Microsoft.AspNetCore.Mvc;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly BluePrismDbReader _bpReader;
    private readonly ILogger<QueueController> _logger;

    public QueueController(BluePrismDbReader bpReader, ILogger<QueueController> logger)
    {
        _bpReader = bpReader;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<QueueSummaryDto>>> GetQueues()
    {
        var summaries = await _bpReader.GetWorkQueueSummariesAsync();

        var dtos = summaries.Select(q => new QueueSummaryDto
        {
            Name = q.Name,
            Pending = q.PendingCount,
            Locked = q.LockedCount,
            Completed = q.CompletedCount,
            Exceptioned = q.ExceptionedCount,
            Total = q.TotalCount,
            HealthPercent = q.TotalCount > 0
                ? Math.Round((double)q.CompletedCount / q.TotalCount * 100, 1)
                : 100
        }).ToList();

        return Ok(dtos);
    }
}
