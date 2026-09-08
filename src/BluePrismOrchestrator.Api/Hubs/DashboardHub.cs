using Microsoft.AspNetCore.SignalR;

namespace BluePrismOrchestrator.Api.Hubs;

/// <summary>
/// Real-time SignalR hub for orchestrator dashboard updates.
/// Streams live bot states, queue depths, session changes, AI recommendations, and alerts.
/// </summary>
public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "DashboardWatchers");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DashboardWatchers");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can request an immediate state push.
    /// </summary>
    public async Task RequestRefresh()
    {
        _logger.LogDebug("Client {ConnectionId} requested immediate refresh", Context.ConnectionId);
        await Clients.Caller.SendAsync("RefreshRequested");
    }

    /// <summary>
    /// Join process-specific channel for granular execution updates.
    /// </summary>
    public async Task JoinProcessGroup(string processName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Process_{processName}");
    }

    /// <summary>
    /// Leave process-specific channel.
    /// </summary>
    public async Task LeaveProcessGroup(string processName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Process_{processName}");
    }
}
