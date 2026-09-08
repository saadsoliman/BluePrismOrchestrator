using Microsoft.AspNetCore.Mvc;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Services.AI;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IPredictiveAnalyticsService _predictiveAnalytics;
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IRecommendationEngine recommendationEngine,
        IPredictiveAnalyticsService predictiveAnalytics,
        IAnomalyDetectionService anomalyService,
        ILogger<AIController> logger)
    {
        _recommendationEngine = recommendationEngine;
        _predictiveAnalytics = predictiveAnalytics;
        _anomalyService = anomalyService;
        _logger = logger;
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<List<AIRecommendationDto>>> GetRecommendations()
    {
        var recs = await _recommendationEngine.GetActiveRecommendationsAsync();
        return Ok(recs);
    }

    [HttpPost("recommendations/{id}/accept")]
    public async Task<IActionResult> AcceptRecommendation(Guid id)
    {
        var success = await _recommendationEngine.AcceptRecommendationAsync(id);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("recommendations/{id}/dismiss")]
    public async Task<IActionResult> DismissRecommendation(Guid id)
    {
        var success = await _recommendationEngine.DismissRecommendationAsync(id);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }

    [HttpGet("failure-heatmap")]
    public async Task<ActionResult<List<FailurePredictionDto>>> GetFailureHeatmap()
    {
        var heatmap = await _predictiveAnalytics.GetFailureHeatmapAsync();
        return Ok(heatmap);
    }

    [HttpGet("capacity-forecast")]
    public async Task<ActionResult<List<CapacityForecastDto>>> GetCapacityForecast()
    {
        var forecast = await _predictiveAnalytics.GetCapacityForecastAsync();
        return Ok(forecast);
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<List<AnomalyDto>>> GetAnomalies([FromQuery] int limit = 20)
    {
        var anomalies = await _anomalyService.GetRecentAnomaliesAsync(limit);
        return Ok(anomalies);
    }

    [HttpPost("retrain")]
    public async Task<IActionResult> RetrainModels()
    {
        _logger.LogInformation("Manual ML retraining triggered");
        await _predictiveAnalytics.TrainModelsAsync();
        await _recommendationEngine.GenerateRecommendationsAsync();
        return Ok(new { status = "Retrained" });
    }
}
