using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BluePrismOrchestrator.Api.Configuration;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LLMController : ControllerBase
{
    private readonly LLMOptions _options;

    public LLMController(IOptions<LLMOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("models")]
    public IActionResult GetAvailableModels()
    {
        var allModels = new List<object>();

        foreach (var m in _options.AvailableModels)
        {
            allModels.Add(new
            {
                id = m.Id,
                name = m.Name,
                maxTokens = m.MaxTokens,
                provider = _options.Provider,
                isCustom = false,
                isLocal = false,
            });
        }

        foreach (var e in _options.CustomEndpoints)
        {
            allModels.Add(new
            {
                id = e.Id,
                name = e.Name,
                model = e.Model,
                url = e.Url,
                provider = e.Provider ?? "Custom",
                isCustom = true,
                isLocal = e.Url.Contains("localhost") || e.Url.Contains("127.0.0.1"),
            });
        }

        return Ok(new
        {
            currentModel = _options.DefaultModel,
            currentProvider = _options.Provider,
            baseUrl = _options.BaseUrl,
            maxTokens = _options.MaxTokens,
            temperature = _options.Temperature,
            models = allModels,
        });
    }

    [HttpPost("model")]
    public IActionResult SetActiveModel([FromBody] SetActiveModelRequest request)
    {
        // In this read-only config setup, we return the selected model info.
        // A full implementation would persist this to the database or update config at runtime.
        var allModels = _options.AvailableModels
            .Select(m => new { id = m.Id, name = m.Name, provider = _options.Provider, isCustom = false, isLocal = false })
            .Cast<object>()
            .Concat(_options.CustomEndpoints.Select(e => new
            {
                id = e.Id,
                name = e.Name,
                model = e.Model,
                url = e.Url,
                provider = e.Provider ?? "Custom",
                isCustom = true,
                isLocal = e.Url.Contains("localhost") || e.Url.Contains("127.0.0.1"),
            }))
            .ToList();

        var selected = allModels
            .FirstOrDefault(m => {
                var id = m.GetType().GetProperty("id")?.GetValue(m)?.ToString();
                return id == request.ModelId;
            });

        if (selected == null)
        {
            return NotFound($"Model '{request.ModelId}' not found in available models");
        }

        return Ok(new
        {
            success = true,
            activeModel = selected,
            message = $"Active LLM model set to '{request.ModelId}'. Note: Full runtime switching requires implementation of config persistence."
        });
    }

    public class SetActiveModelRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
    }
}
