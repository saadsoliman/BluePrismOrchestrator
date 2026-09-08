namespace BluePrismOrchestrator.Api.Models.BluePrism;

/// <summary>Maps to BPAProcess table (read-only)</summary>
public class BPProcess
{
    public Guid ProcessId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>'P' = Process, 'O' = Object</summary>
    public string ProcessType { get; set; } = "P";
    public string? Description { get; set; }
}

/// <summary>Maps to BPASession table (read-only)</summary>
public class BPSession
{
    public Guid SessionId { get; set; }
    public int? SessionNumber { get; set; }
    public Guid ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public int StatusId { get; set; }
    public string? StatusDescription { get; set; }
    public Guid? StarterResourceId { get; set; }
    public Guid? RunningResourceId { get; set; }
    public string? ResourceName { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}

/// <summary>Maps to BPAResource table (read-only)</summary>
public class BPResource
{
    public Guid ResourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ProcessesRunning { get; set; }
    public int? ActionsRunning { get; set; }
    /// <summary>Derived in DbReader: true when BP statusid=1 (Connected) or any session running.</summary>
    public bool IsOnline { get; set; }
    public int StatusId { get; set; }
    public string? DisplayStatus { get; set; }
    public string? PoolName { get; set; }
}

/// <summary>Maps to BPAWorkQueue table (read-only)</summary>
public class BPWorkQueue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int LockedCount { get; set; }
    public int CompletedCount { get; set; }
    public int ExceptionedCount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>Maps to BPAWorkQueueItem table (read-only)</summary>
public class BPWorkQueueItem
{
    public Guid Id { get; set; }
    public Guid QueueId { get; set; }
    public string? QueueName { get; set; }
    public string? KeyValue { get; set; }
    public string? Status { get; set; }
    public int Attempt { get; set; }
    public DateTime? Finished { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? LockTime { get; set; }
    public string? ExceptionReason { get; set; }
    public Guid? SessionId { get; set; }
}

/// <summary>Maps to BPAStatus table (read-only)</summary>
public static class BPStatusMap
{
    public const int Pending = 0;
    public const int Running = 1;
    public const int Terminated = 2;
    public const int Stopped = 3;
    public const int Completed = 4;
    public const int Debugging = 5;
    public const int Archived = 6;
    public const int Stopping = 7;

    public static string GetDescription(int statusId) => statusId switch
    {
        Pending => "Pending",
        Running => "Running",
        Terminated => "Terminated",
        Stopped => "Stopped",
        Completed => "Completed",
        Debugging => "Exceptioned",
        Archived => "Archived",
        Stopping => "Stopping",
        _ => "Unknown"
    };
}
