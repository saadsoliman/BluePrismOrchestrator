namespace BluePrismOrchestrator.Api.Configuration;

public class LLMOptions
{
    public const string SectionName = "LLM";

    public string Provider { get; set; } = "OpenAI";
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string DefaultModel { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;

    // List of available model options for dynamic selector
    public List<LLMModelOption> AvailableModels { get; set; } = new();

    // Custom model entries allowing user-configured local or external LLMs
    public List<CustomLLMEndpoint> CustomEndpoints { get; set; } = new();
}

public class LLMModelOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
}

public class CustomLLMEndpoint
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? Provider { get; set; } = "Custom";
}
