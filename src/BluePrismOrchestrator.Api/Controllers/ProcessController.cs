using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Services;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessController : ControllerBase
{
    private readonly BluePrismDbReader _bpReader;
    private readonly OrchestratorDbContext _db;
    private readonly IAutomateCService _automateC;
    private readonly IWorkloadBalancerService _workloadBalancer;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(
        BluePrismDbReader bpReader,
        OrchestratorDbContext db,
        IAutomateCService automateC,
        IWorkloadBalancerService workloadBalancer,
        ILogger<ProcessController> logger)
    {
        _bpReader = bpReader;
        _db = db;
        _automateC = automateC;
        _workloadBalancer = workloadBalancer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProcessDto>>> GetProcesses()
    {
        var bpProcesses = (await _bpReader.GetProcessesAsync()).ToList();

        var metrics = await _db.ProcessMetrics.ToListAsync();
        var metricsMap = metrics.ToDictionary(m => m.ProcessName, m => m);

        var dtos = bpProcesses.Select(p =>
        {
            metricsMap.TryGetValue(p.Name, out var metric);
            return new ProcessDto
            {
                ProcessId = p.ProcessId,
                Name = p.Name,
                Description = p.ProcessType switch
                {
                    "P" => "Standard Automation Process",
                    "O" => "Business Object",
                    _ => "RPA Process"
                },
                RecentExecutions = metric?.TotalExecutions ?? 0,
                SuccessRate = metric != null && metric.TotalExecutions > 0
                    ? Math.Round((double)metric.SuccessfulExecutions / metric.TotalExecutions * 100, 1)
                    : 98.0,
                AvgDurationSeconds = metric?.AvgDurationSeconds ?? 145.0
            };
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{name}/run")]
    public async Task<ActionResult<ProcessRunResponseDto>> RunProcess(
        string name,
        [FromBody] ProcessRunRequestDto? request)
    {
        _logger.LogInformation("Manual trigger requested for process '{ProcessName}'", name);

        var targetResource = request?.TargetResource;
        if (string.IsNullOrEmpty(targetResource))
        {
            targetResource = await _workloadBalancer.GetOptimalResourceAsync(name);
        }

        var execution = new ProcessExecution
        {
            ProcessName = name,
            ResourceName = targetResource,
            Status = ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow,
            AiTriggered = false
        };

        _db.Executions.Add(execution);
        await _db.SaveChangesAsync();

        // Trigger execution asynchronously via AutomateC
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _automateC.RunProcessAsync(name, targetResource, request?.Parameters);

                using var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
                var exec = await db.Executions.FindAsync(execution.Id);
                if (exec != null)
                {
                    exec.CompletedAt = DateTime.UtcNow;
                    exec.DurationSeconds = (exec.CompletedAt.Value - exec.StartedAt!.Value).TotalSeconds;
                    exec.Status = result.Success ? ExecutionStatus.Completed : ExecutionStatus.Failed;
                    exec.ErrorMessage = result.Error;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background run failed for execution {Id}", execution.Id);
            }
        });

        return Accepted(new ProcessRunResponseDto
        {
            ExecutionId = execution.Id,
            Status = "Initiated",
            Message = $"Process execution dispatched to resource '{targetResource ?? "Auto-assigned"}'"
        });
    }
}
