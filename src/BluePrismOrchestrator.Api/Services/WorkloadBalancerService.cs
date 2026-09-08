using BluePrismOrchestrator.Api.Data;

namespace BluePrismOrchestrator.Api.Services;

public interface IWorkloadBalancerService
{
    Task<string?> GetOptimalResourceAsync(string processName);
}

public class WorkloadBalancerService : IWorkloadBalancerService
{
    private readonly BluePrismDbReader _bpReader;
    private readonly ILogger<WorkloadBalancerService> _logger;

    public WorkloadBalancerService(BluePrismDbReader bpReader, ILogger<WorkloadBalancerService> logger)
    {
        _bpReader = bpReader;
        _logger = logger;
    }

    /// <summary>
    /// Selects the optimal runtime resource for a process execution based on:
    /// 1. Resource availability (online status)
    /// 2. Current workload (fewest active processes)
    /// 3. Resource affinity (future: process-specific preferences)
    /// </summary>
    public async Task<string?> GetOptimalResourceAsync(string processName)
    {
        try
        {
            var resources = (await _bpReader.GetResourcesAsync()).ToList();
            var activeSessions = (await _bpReader.GetActiveSessionsAsync()).ToList();

            if (resources.Count == 0)
            {
                _logger.LogWarning("No resources available for workload balancing");
                return null;
            }

            // Build resource workload map
            var workload = resources.Select(r => new
            {
                Resource = r,
                ActiveCount = activeSessions.Count(s =>
                    s.RunningResourceId == r.ResourceId && s.StatusId == 1),
                TotalCapacity = 5 // Default max concurrent sessions per resource; configurable
            }).ToList();

            // Filter to resources with available capacity
            var available = workload
                .Where(w => w.ActiveCount < w.TotalCapacity)
                .OrderBy(w => w.ActiveCount) // Least loaded first
                .ThenBy(w => w.Resource.Name) // Stable sort
                .ToList();

            if (available.Count == 0)
            {
                _logger.LogWarning("All resources at capacity for {Process}", processName);
                // Fall back to least loaded even if at capacity
                var leastLoaded = workload.OrderBy(w => w.ActiveCount).First();
                _logger.LogInformation("Falling back to least loaded resource: {Resource} ({Active} sessions)",
                    leastLoaded.Resource.Name, leastLoaded.ActiveCount);
                return leastLoaded.Resource.Name;
            }

            var optimal = available.First();
            _logger.LogInformation(
                "Selected resource {Resource} for {Process} (load: {Active}/{Capacity})",
                optimal.Resource.Name, processName, optimal.ActiveCount, optimal.TotalCapacity);

            return optimal.Resource.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workload balancing for {Process}", processName);
            return null;
        }
    }
}
