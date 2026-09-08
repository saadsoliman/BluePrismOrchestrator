namespace BluePrismOrchestrator.Api.Configuration;

public class AlertOptions
{
    public const string SectionName = "Alerts";

    public SmtpSettings? Smtp { get; set; }
    public TeamsSettings? Teams { get; set; }
    public WebhookSettings? Webhook { get; set; }
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
}

public class TeamsSettings
{
    public string WebhookUrl { get; set; } = string.Empty;
}

public class WebhookSettings
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
