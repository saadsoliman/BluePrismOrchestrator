using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using BluePrismOrchestrator.Api.Configuration;

namespace BluePrismOrchestrator.Api.Services;

public class ExecutionResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class ResourceStatusResult
{
    public bool Success { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string RawOutput { get; set; } = string.Empty;
    public List<string> SessionInfo { get; set; } = new();
}

public interface IAutomateCService
{
    Task<ExecutionResult> RunProcessAsync(string processName, string? resource = null, Dictionary<string, string>? parameters = null);
    Task<ExecutionResult> RequestStopAsync(string processName, string resource);
    Task<ExecutionResult> RequestStopBySessionIdAsync(Guid sessionId);
    Task<ResourceStatusResult> GetResourceStatusAsync(string resourceName);
}

public class AutomateCService : IAutomateCService
{
    private readonly AutomateCOptions _options;
    private readonly ILogger<AutomateCService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public AutomateCService(IOptions<AutomateCOptions> options, ILogger<AutomateCService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentInvocations, _options.MaxConcurrentInvocations);
    }

    public async Task<ExecutionResult> RunProcessAsync(
        string processName,
        string? resource = null,
        Dictionary<string, string>? parameters = null)
    {
        var args = new StringBuilder();
        args.Append($"/run \"{processName}\"");

        // Authentication
        AppendAuthArgs(args);

        // Target resource
        if (!string.IsNullOrEmpty(resource))
        {
            args.Append($" /resource \"{resource}\"");
        }

        // Startup parameters
        if (parameters?.Count > 0)
        {
            var xml = BuildStartupParametersXml(parameters);
            args.Append($" /startp \"{xml}\"");
        }

        return await ExecuteCommandAsync(args.ToString(), $"Run:{processName}");
    }

    public async Task<ExecutionResult> RequestStopAsync(string processName, string resource)
    {
        var args = new StringBuilder();
        args.Append($"/requeststop \"{processName}\" \"{resource}\"");
        AppendAuthArgs(args);

        return await ExecuteCommandAsync(args.ToString(), $"Stop:{processName}@{resource}");
    }

    public async Task<ExecutionResult> RequestStopBySessionIdAsync(Guid sessionId)
    {
        var args = new StringBuilder();
        args.Append($"/requeststopbyid \"{sessionId}\"");
        AppendAuthArgs(args);

        return await ExecuteCommandAsync(args.ToString(), $"Stop:SessionId://{sessionId}");
    }

    public async Task<ResourceStatusResult> GetResourceStatusAsync(string resourceName)
    {
        var args = new StringBuilder();
        args.Append($"/resourcestatus \"{resourceName}\"");
        AppendAuthArgs(args);

        var result = await ExecuteCommandAsync(args.ToString(), $"Status:{resourceName}");

        return new ResourceStatusResult
        {
            Success = result.Success,
            ResourceName = resourceName,
            RawOutput = result.Output,
            SessionInfo = result.Output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
        };
    }

    private void AppendAuthArgs(StringBuilder args)
    {
        // Database connection name
        if (!string.IsNullOrEmpty(_options.DbConnectionName))
        {
            args.Append($" /dbconname \"{_options.DbConnectionName}\"");
        }

        // Authentication mode
        if (_options.AuthMode.Equals("SSO", StringComparison.OrdinalIgnoreCase))
        {
            args.Append(" /sso");
        }
        else if (!string.IsNullOrEmpty(_options.Username))
        {
            args.Append($" /user {_options.Username} {_options.Password}");
        }
    }

    private static string BuildStartupParametersXml(Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder("<inputs>");
        foreach (var (key, value) in parameters)
        {
            // Use single quotes for XML attributes to avoid conflicts with command-line double quotes
            sb.Append($" <input name='{key}' value='{value}' />");
        }
        sb.Append(" </inputs>");
        return sb.ToString();
    }

    private async Task<ExecutionResult> ExecuteCommandAsync(string arguments, string operationName)
    {
        await _semaphore.WaitAsync();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("AutomateC [{Operation}]: Starting with args: {Args}",
                operationName, MaskSensitiveArgs(arguments));

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await Task.Run(() =>
                process.WaitForExit(_options.TimeoutSeconds * 1000));

            if (!completed)
            {
                _logger.LogWarning("AutomateC [{Operation}]: Timed out after {Timeout}s, killing process",
                    operationName, _options.TimeoutSeconds);
                process.Kill(entireProcessTree: true);
                await Task.Run(() => process.WaitForExit(5000));
            }

            stopwatch.Stop();
            var result = new ExecutionResult
            {
                Success = completed && process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = stdoutBuilder.ToString().Trim(),
                Error = stderrBuilder.ToString().Trim(),
                Duration = stopwatch.Elapsed
            };

            _logger.LogInformation(
                "AutomateC [{Operation}]: Completed in {Duration}ms, ExitCode={ExitCode}, Success={Success}",
                operationName, stopwatch.ElapsedMilliseconds, result.ExitCode, result.Success);

            if (!result.Success)
            {
                _logger.LogWarning("AutomateC [{Operation}]: stderr: {Error}", operationName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AutomateC [{Operation}]: Exception during execution", operationName);
            return new ExecutionResult
            {
                Success = false,
                ExitCode = -1,
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string MaskSensitiveArgs(string args)
    {
        // Mask password in /user username password pattern
        var parts = args.Split(' ');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "/user" && i + 2 < parts.Length)
            {
                parts[i + 2] = "****";
            }
        }
        return string.Join(' ', parts);
    }
}
