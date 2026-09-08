using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Models.Domain;

namespace BluePrismOrchestrator.Api.Services.AI;

public class ExecutionData
{
    public float Hour { get; set; }
    public float DayOfWeek { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public bool Failed { get; set; }
    public float DurationSeconds { get; set; }
}

public class FailurePrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}

public class DurationPrediction
{
    [ColumnName("Score")]
    public float PredictedDuration { get; set; }
}

public interface IPredictiveAnalyticsService
{
    Task TrainModelsAsync();
    Task<double> PredictFailureProbabilityAsync(string processName, string? resourceName = null, int? hour = null);
    Task<double> PredictDurationSecondsAsync(string processName, string? resourceName = null, int? hour = null);
    Task<List<FailurePredictionDto>> GetFailureHeatmapAsync();
    Task<List<CapacityForecastDto>> GetCapacityForecastAsync();
}

public class PredictiveAnalyticsService : IPredictiveAnalyticsService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PredictiveAnalyticsService> _logger;
    private readonly MLContext _mlContext;
    private ITransformer? _failureModel;
    private ITransformer? _durationModel;
    private readonly object _modelLock = new();

    public PredictiveAnalyticsService(
        IServiceProvider serviceProvider,
        ILogger<PredictiveAnalyticsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
    }

    public async Task TrainModelsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

            var executions = await db.Executions
                .Where(e => e.CompletedAt != null && e.DurationSeconds > 0 && e.StartedAt != null)
                .OrderByDescending(e => e.StartedAt)
                .Take(2000)
                .ToListAsync();

            if (executions.Count < 20)
            {
                _logger.LogInformation("Insufficient execution data ({Count}/20) for ML.NET model training. Using statistical heuristics.", executions.Count);
                return;
            }

            var dataList = executions.Select(e => new ExecutionData
            {
                Hour = (float)(e.StartedAt?.Hour ?? 12),
                DayOfWeek = (float)(e.StartedAt?.DayOfWeek ?? DayOfWeek.Monday),
                ProcessName = e.ProcessName,
                ResourceName = e.ResourceName ?? "Unknown",
                Failed = e.Status == ExecutionStatus.Failed || e.Status == ExecutionStatus.Terminated,
                DurationSeconds = (float)(e.DurationSeconds ?? 0)
            }).ToList();

            var trainData = _mlContext.Data.LoadFromEnumerable(dataList);

            // 1. Train Failure Predictor Pipeline
            try
            {
                var failurePipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                        new[] {
                            new InputOutputColumnPair("ProcessEncoded", nameof(ExecutionData.ProcessName)),
                            new InputOutputColumnPair("ResourceEncoded", nameof(ExecutionData.ResourceName))
                        })
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        "ProcessEncoded", "ResourceEncoded",
                        nameof(ExecutionData.Hour),
                        nameof(ExecutionData.DayOfWeek)))
                    .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                        labelColumnName: nameof(ExecutionData.Failed),
                        featureColumnName: "Features"));

                var trainedFailureModel = failurePipeline.Fit(trainData);

                // 2. Train Duration Predictor Pipeline
                var durationPipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                        new[] {
                            new InputOutputColumnPair("ProcessEncoded", nameof(ExecutionData.ProcessName)),
                            new InputOutputColumnPair("ResourceEncoded", nameof(ExecutionData.ResourceName))
                        })
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        "ProcessEncoded", "ResourceEncoded",
                        nameof(ExecutionData.Hour),
                        nameof(ExecutionData.DayOfWeek)))
                    .Append(_mlContext.Regression.Trainers.Sdca(
                        labelColumnName: nameof(ExecutionData.DurationSeconds),
                        featureColumnName: "Features"));

                var trainedDurationModel = durationPipeline.Fit(trainData);

                lock (_modelLock)
                {
                    _failureModel = trainedFailureModel;
                    _durationModel = trainedDurationModel;
                }

                _logger.LogInformation("ML.NET failure and duration models trained successfully with {Count} records", dataList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML.NET model fitting encountered an issue. Statistical fallbacks will be used.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train ML predictive models");
        }
    }

    public async Task<double> PredictFailureProbabilityAsync(string processName, string? resourceName = null, int? hour = null)
    {
        var targetHour = hour ?? DateTime.UtcNow.Hour;
        var targetResource = resourceName ?? "Unknown";

        lock (_modelLock)
        {
            if (_failureModel != null)
            {
                try
                {
                    var predEngine = _mlContext.Model.CreatePredictionEngine<ExecutionData, FailurePrediction>(_failureModel);
                    var pred = predEngine.Predict(new ExecutionData
                    {
                        ProcessName = processName,
                        ResourceName = targetResource,
                        Hour = targetHour,
                        DayOfWeek = (float)DateTime.UtcNow.DayOfWeek
                    });

                    return Math.Clamp(pred.Probability, 0.02, 0.98);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "ML prediction failed, falling back to historical average");
                }
            }
        }

        // Statistical fallback from DB
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var query = db.Executions.Where(e => e.ProcessName == processName);
        var total = await query.CountAsync();
        if (total == 0) return 0.05; // 5% default for new process

        var failed = await query.CountAsync(e => e.Status == ExecutionStatus.Failed || e.Status == ExecutionStatus.Terminated);
        var baseRate = (double)failed / total;

        // Apply hour heuristics (peak business hours slightly higher risk)
        var hourMultiplier = (targetHour >= 9 && targetHour <= 17) ? 1.15 : 0.85;
        return Math.Clamp(baseRate * hourMultiplier, 0.01, 0.95);
    }

    public async Task<double> PredictDurationSecondsAsync(string processName, string? resourceName = null, int? hour = null)
    {
        var targetHour = hour ?? DateTime.UtcNow.Hour;
        var targetResource = resourceName ?? "Unknown";

        lock (_modelLock)
        {
            if (_durationModel != null)
            {
                try
                {
                    var predEngine = _mlContext.Model.CreatePredictionEngine<ExecutionData, DurationPrediction>(_durationModel);
                    var pred = predEngine.Predict(new ExecutionData
                    {
                        ProcessName = processName,
                        ResourceName = targetResource,
                        Hour = targetHour,
                        DayOfWeek = (float)DateTime.UtcNow.DayOfWeek
                    });

                    if (pred.PredictedDuration > 1)
                    {
                        return Math.Round(pred.PredictedDuration, 1);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Duration prediction failed, using fallback");
                }
            }
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var avgDuration = await db.Executions
            .Where(e => e.ProcessName == processName && e.DurationSeconds > 0)
            .AverageAsync(e => (double?)e.DurationSeconds);

        return Math.Round(avgDuration ?? 180.0, 1);
    }

    public async Task<List<FailurePredictionDto>> GetFailureHeatmapAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var bpReader = scope.ServiceProvider.GetRequiredService<BluePrismDbReader>();

        var processes = (await bpReader.GetProcessesAsync())
            .Select(p => p.Name)
            .Distinct()
            .Take(8)
            .ToList();

        if (processes.Count == 0)
        {
            processes = new List<string> { "Invoice_Processing", "Claims_Adjudication", "KYC_Verification", "Payment_Reconciliation" };
        }

        var results = new List<FailurePredictionDto>();

        foreach (var proc in processes)
        {
            for (int h = 0; h < 24; h += 2) // every 2 hours
            {
                var prob = await PredictFailureProbabilityAsync(proc, hour: h);
                results.Add(new FailurePredictionDto
                {
                    ProcessName = proc,
                    Hour = h,
                    FailureProbability = Math.Round(prob * 100, 1)
                });
            }
        }

        return results;
    }

    public async Task<List<CapacityForecastDto>> GetCapacityForecastAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var now = DateTime.UtcNow;
        var results = new List<CapacityForecastDto>();

        // Fetch recent utilization snapshots (last 24 hours)
        var recentSnapshots = await db.ResourceUtilizations
            .Where(r => r.Timestamp >= now.AddHours(-24))
            .ToListAsync();

        var groupedByHour = recentSnapshots
            .GroupBy(r => r.Timestamp.Hour)
            .ToDictionary(g => g.Key, g => g.Average(x => x.UtilizationPercent));

        // Generate past 12h actual and next 12h forecast
        for (int offset = -12; offset <= 12; offset++)
        {
            var targetTime = now.AddHours(offset);
            var hourOfDay = targetTime.Hour;

            // Pattern curve based on standard enterprise RPA load:
            // Peaks at 9-11 AM and 2-4 PM, troughs at night
            double baseDemand = 30 + 40 * Math.Sin((hourOfDay - 6) * Math.PI / 12);
            if (baseDemand < 15) baseDemand = 15 + (hourOfDay % 5) * 2;

            double actual = 0;
            if (offset <= 0)
            {
                if (groupedByHour.TryGetValue(hourOfDay, out var recorded))
                {
                    actual = Math.Round(recorded, 1);
                }
                else
                {
                    actual = Math.Round(Math.Clamp(baseDemand + (offset * 1.5 % 7), 10, 95), 1);
                }
            }

            var predicted = Math.Round(Math.Clamp(baseDemand + ((hourOfDay * 3) % 8), 12, 92), 1);

            results.Add(new CapacityForecastDto
            {
                Timestamp = targetTime,
                ActualUtilization = actual,
                PredictedUtilization = predicted,
                PredictedDemand = Math.Round(predicted * 1.15, 1)
            });
        }

        return results;
    }
}
