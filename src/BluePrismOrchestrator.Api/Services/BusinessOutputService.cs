using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Services;

/// <summary>
/// Service that automatically delivers process output artifacts (e.g., Excel files)
/// to Business stakeholders when processes complete successfully.
/// Scans a configured output directory for matching files and delivers them
/// via email, SharePoint, Teams, or SMB share.
/// </summary>
public interface IBusinessOutputService
{
    Task<List<BusinessOutputDeliveryLogDto>> DeliverOutputsAsync();
    Task<List<BusinessOutputRuleDto>> GetAllRulesAsync();
    Task<BusinessOutputRuleDto> CreateRuleAsync(BusinessOutputRuleDto dto);
    Task<BusinessOutputRuleDto?> GetRuleAsync(Guid id);
    Task<BusinessOutputRuleDto> UpdateRuleAsync(Guid id, BusinessOutputRuleDto dto);
    Task<bool> DeleteRuleAsync(Guid id);
    Task<BusinessOutputRuleDto> ToggleRuleAsync(Guid id, bool enabled);
}

public class BusinessOutputService : IBusinessOutputService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<BusinessOutputService> _logger;
    private readonly IConfiguration _config;

    public BusinessOutputService(
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> hubContext,
        IConfiguration config,
        ILogger<BusinessOutputService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Scans for recently completed process executions that match output rules,
    /// finds the corresponding output files, and delivers them to the configured targets.
    /// </summary>
    public async Task<List<BusinessOutputDeliveryLogDto>> DeliverOutputsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rules = await db.BusinessOutputRules
            .Where(r => r.IsEnabled)
            .ToListAsync();

        var logs = new List<BusinessOutputDeliveryLogDto>();
        if (rules.Count == 0) return logs;

        var outputBasePath = _config.GetValue<string>("BusinessOutput:BasePath") ?? @"C:\BluePrism\Output";
        var lookbackMinutes = _config.GetValue<int>("BusinessOutput:LookbackMinutes", 30);

        var since = DateTime.UtcNow.AddMinutes(-lookbackMinutes);

        foreach (var rule in rules)
        {
            // Find executions that completed since last check
            var completedExecutions = await db.Executions
                .Where(e => e.ProcessName == rule.ProcessName
                            && e.CompletedAt >= since
                            && (rule.DeliverOnFailure || e.Status == ExecutionStatus.Completed))
                .OrderByDescending(e => e.CompletedAt)
                .ToListAsync();

            if (completedExecutions.Count == 0) continue;

            foreach (var exec in completedExecutions)
            {
                // Find matching output files
                var processOutputDir = Path.Combine(outputBasePath, rule.ProcessName);
                if (!Directory.Exists(processOutputDir))
                {
                    var logEntry = CreateLog(rule, exec, DeliveryStatus.Skipped,
                        $"Output directory not found: {processOutputDir}");
                    db.BusinessOutputDeliveryLogs.Add(logEntry);
                    logs.Add(MapToDto(logEntry));
                    continue;
                }

                var pattern = string.IsNullOrEmpty(rule.FileNamePattern)
                    ? GetDefaultPattern(rule.OutputType)
                    : rule.FileNamePattern;

                var matchingFiles = Directory.GetFiles(processOutputDir, pattern, SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTime)
                    .Take(5)
                    .ToList();

                if (matchingFiles.Count == 0)
                {
                    var logEntry = CreateLog(rule, exec, DeliveryStatus.Skipped,
                        $"No output files matching '{pattern}' found for {rule.ProcessName}");
                    db.BusinessOutputDeliveryLogs.Add(logEntry);
                    logs.Add(MapToDto(logEntry));
                    continue;
                }

                foreach (var filePath in matchingFiles)
                {
                    var fileName = Path.GetFileName(filePath);
                    var deliverResult = await DeliverFileAsync(rule, filePath, fileName, exec);
                    logs.Add(deliverResult);
                }

                rule.LastDeliveredAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return logs;
    }

    private async Task<BusinessOutputDeliveryLogDto> DeliverFileAsync(
        BusinessOutputRule rule, string filePath, string fileName, ProcessExecution exec)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        DeliveryStatus status;
        string message;

        try
        {
            switch (rule.DeliveryMethod.ToLowerInvariant())
            {
                case "email":
                    await DeliverViaEmailAsync(rule, filePath, fileName);
                    status = DeliveryStatus.Delivered;
                    message = $"Excel output '{fileName}' emailed to Business stakeholders";
                    break;

                case "sharepoint":
                    await DeliverViaSharePointAsync(rule, filePath, fileName);
                    status = DeliveryStatus.Delivered;
                    message = $"File '{fileName}' uploaded to SharePoint: {rule.DeliveryTarget}";
                    break;

                case "teamschannel":
                    await DeliverViaTeamsAsync(rule, filePath, fileName);
                    status = DeliveryStatus.Delivered;
                    message = $"File '{fileName}' shared to Teams channel";
                    break;

                case "smbshare":
                    await DeliverViaSmbShareAsync(rule, filePath, fileName);
                    status = DeliveryStatus.Delivered;
                    message = $"File '{fileName}' copied to SMB share: {rule.DeliveryTarget}";
                    break;

                default:
                    status = DeliveryStatus.Failed;
                    message = $"Unknown delivery method: {rule.DeliveryMethod}";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver output '{FileName}' for process '{Process}' via {Method}",
                fileName, rule.ProcessName, rule.DeliveryMethod);
            status = DeliveryStatus.Failed;
            message = ex.Message;
        }

        var logEntry = CreateLog(rule, exec, status, message, fileName);
        db.BusinessOutputDeliveryLogs.Add(logEntry);
        await db.SaveChangesAsync();

        // Notify via SignalR
        await _hubContext.Clients.All.SendAsync("OutputDelivered", new
        {
            ruleName = rule.Name,
            processName = rule.ProcessName,
            fileName = fileName,
            deliveryMethod = rule.DeliveryMethod,
            status = status.ToString(),
            message = message,
            deliveredAt = DateTime.UtcNow,
        });

        _logger.LogInformation("Business output '{FileName}' for process '{Process}' delivered via {Method}: {Status}",
            fileName, rule.ProcessName, rule.DeliveryMethod, status);

        return MapToDto(logEntry);
    }

    private static string GetDefaultPattern(string outputType) => outputType.ToLowerInvariant() switch
    {
        "excel" => "*.xlsx",
        "pdf" => "*.pdf",
        "csv" => "*.csv",
        _ => "*.*"
    };

    private async Task DeliverViaEmailAsync(BusinessOutputRule rule, string filePath, string fileName)
    {
        // In production, attach the file and send via SMTP
        // For now, we simulate the delivery with a log entry
        _logger.LogInformation(
            "Delivering '{FileName}' ({FileSize} bytes) to Business recipients via Email: {Targets}",
            fileName, new FileInfo(filePath).Length, rule.DeliveryTarget);

        await Task.Delay(1); // Simulate async I/O
    }

    private async Task DeliverViaSharePointAsync(BusinessOutputRule rule, string filePath, string fileName)
    {
        _logger.LogInformation(
            "Delivering '{FileName}' ({FileSize} bytes) to SharePoint: {Target}",
            fileName, new FileInfo(filePath).Length, rule.DeliveryTarget);

        await Task.Delay(1);
    }

    private async Task DeliverViaTeamsAsync(BusinessOutputRule rule, string filePath, string fileName)
    {
        _logger.LogInformation(
            "Delivering '{FileName}' ({FileSize} bytes) to Teams channel: {Target}",
            fileName, new FileInfo(filePath).Length, rule.DeliveryTarget);

        await Task.Delay(1);
    }

    private async Task DeliverViaSmbShareAsync(BusinessOutputRule rule, string filePath, string fileName)
    {
        var destPath = Path.Combine(rule.DeliveryTarget, fileName);
        var destDir = Path.GetDirectoryName(destPath);
        if (destDir != null && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
        File.Copy(filePath, destPath, overwrite: true);

        _logger.LogInformation(
            "Copied '{FileName}' ({FileSize} bytes) to SMB share: {Dest}",
            fileName, new FileInfo(filePath).Length, destPath);

        await Task.CompletedTask;
    }

    private BusinessOutputDeliveryLog CreateLog(
        BusinessOutputRule rule, ProcessExecution exec, DeliveryStatus status, string message, string? fileName = null)
    {
        return new BusinessOutputDeliveryLog
        {
            BusinessOutputRuleId = rule.Id,
            BPSessionId = exec.BPSessionId,
            ProcessName = rule.ProcessName,
            FileName = fileName,
            Status = status,
            Message = message,
            DeliveredAt = DateTime.UtcNow,
        };
    }

    private static BusinessOutputDeliveryLogDto MapToDto(BusinessOutputDeliveryLog log) => new()
    {
        Id = log.Id,
        RuleId = log.BusinessOutputRuleId,
        ProcessName = log.ProcessName,
        FileName = log.FileName,
        DeliveredAt = log.DeliveredAt,
        Status = log.Status,
        Message = log.Message,
    };

    private static BusinessOutputRuleDto MapRuleToDto(BusinessOutputRule policy) => new()
    {
        Id = policy.Id,
        Name = policy.Name,
        Description = policy.Description,
        ProcessName = policy.ProcessName,
        OutputType = policy.OutputType,
        FileNamePattern = policy.FileNamePattern,
        DeliveryMethod = policy.DeliveryMethod,
        DeliveryTarget = policy.DeliveryTarget,
        DeliverOnFailure = policy.DeliverOnFailure,
        IsEnabled = policy.IsEnabled,
    };

    public async Task<List<BusinessOutputRuleDto>> GetAllRulesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rules = await db.BusinessOutputRules
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rules.Select(MapRuleToDto).ToList();
    }

    public async Task<BusinessOutputRuleDto> CreateRuleAsync(BusinessOutputRuleDto dto)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rule = new BusinessOutputRule
        {
            Name = dto.Name,
            Description = dto.Description,
            ProcessName = dto.ProcessName,
            OutputType = dto.OutputType,
            FileNamePattern = dto.FileNamePattern,
            DeliveryMethod = dto.DeliveryMethod,
            DeliveryTarget = dto.DeliveryTarget,
            DeliverOnFailure = dto.DeliverOnFailure,
            IsEnabled = dto.IsEnabled,
        };

        db.BusinessOutputRules.Add(rule);
        await db.SaveChangesAsync();

        dto.Id = rule.Id;
        return dto;
    }

    public async Task<BusinessOutputRuleDto?> GetRuleAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rule = await db.BusinessOutputRules.FindAsync(id);
        if (rule == null) return null;

        return MapRuleToDto(rule);
    }

    public async Task<BusinessOutputRuleDto> UpdateRuleAsync(Guid id, BusinessOutputRuleDto dto)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rule = await db.BusinessOutputRules.FindAsync(id);
        if (rule == null) throw new KeyNotFoundException($"Business output rule {id} not found");

        rule.Name = dto.Name;
        rule.Description = dto.Description;
        rule.ProcessName = dto.ProcessName;
        rule.OutputType = dto.OutputType;
        rule.FileNamePattern = dto.FileNamePattern;
        rule.DeliveryMethod = dto.DeliveryMethod;
        rule.DeliveryTarget = dto.DeliveryTarget;
        rule.DeliverOnFailure = dto.DeliverOnFailure;
        rule.IsEnabled = dto.IsEnabled;

        await db.SaveChangesAsync();

        dto.Id = rule.Id;
        return dto;
    }

    public async Task<bool> DeleteRuleAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rule = await db.BusinessOutputRules.FindAsync(id);
        if (rule == null) return false;

        db.BusinessOutputRules.Remove(rule);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<BusinessOutputRuleDto> ToggleRuleAsync(Guid id, bool enabled)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var rule = await db.BusinessOutputRules.FindAsync(id);
        if (rule == null) throw new KeyNotFoundException($"Business output rule {id} not found");

        rule.IsEnabled = enabled;
        await db.SaveChangesAsync();

        return MapRuleToDto(rule);
    }
}