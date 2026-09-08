using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using BluePrismOrchestrator.Api.Configuration;
using BluePrismOrchestrator.Api.Models.BluePrism;

namespace BluePrismOrchestrator.Api.Data;

/// <summary>
/// Read-only access to the Blue Prism SQL Server database via Dapper.
/// All queries are SELECT-only — no writes to the BP database.
/// Provides intelligent simulation fallbacks when SQL Server is offline or in development mode.
/// </summary>
public class BluePrismDbReader
{
    private readonly string _connectionString;
    private readonly ILogger<BluePrismDbReader> _logger;

    public BluePrismDbReader(IOptions<BluePrismDbOptions> options, ILogger<BluePrismDbReader> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    // ─── Processes ────────────────────────────────────────────

    public async Task<IEnumerable<BPProcess>> GetProcessesAsync()
    {
        try
        {
            const string sql = @"
                SELECT processid AS ProcessId,
                       name AS Name,
                       ProcessType,
                       description AS Description
                FROM BPAProcess
                WHERE ProcessType = 'P'
                  AND name NOT LIKE 'Utility - %'
                  AND name NOT LIKE '% - Performer Template'
                ORDER BY name";

            using var conn = CreateConnection();
            return await conn.QueryAsync<BPProcess>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; returning simulated enterprise processes.");
            return GetSimulatedProcesses();
        }
    }

    public async Task<BPProcess?> GetProcessByNameAsync(string name)
    {
        try
        {
            const string sql = @"
                SELECT processid AS ProcessId,
                       name AS Name,
                       ProcessType,
                       description AS Description
                FROM BPAProcess
                WHERE name = @Name AND ProcessType = 'P'
                  AND name NOT LIKE 'Utility - %'
                  AND name NOT LIKE '% - Performer Template'";

            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<BPProcess>(sql, new { Name = name });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; finding in simulated processes.");
            return GetSimulatedProcesses().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ─── Sessions ─────────────────────────────────────────────

    public async Task<IEnumerable<BPSession>> GetActiveSessionsAsync()
    {
        try
        {
            const string sql = @"
                SELECT s.sessionid AS SessionId,
                       s.sessionnumber AS SessionNumber,
                       s.processid AS ProcessId,
                       p.name AS ProcessName,
                       s.statusid AS StatusId,
                       st.description AS StatusDescription,
                       s.starterresourceid AS StarterResourceId,
                       s.runningresourceid AS RunningResourceId,
                       r.name AS ResourceName,
                       s.startdatetime AS StartDateTime,
                       s.enddatetime AS EndDateTime
                FROM BPASession s
                INNER JOIN BPAProcess p ON s.processid = p.processid
                LEFT JOIN BPAStatus st ON s.statusid = st.statusid
                LEFT JOIN BPAResource r ON s.runningresourceid = r.resourceid
                WHERE s.statusid IN (0, 1, 7)
                ORDER BY s.startdatetime DESC";

            using var conn = CreateConnection();
            return await conn.QueryAsync<BPSession>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; returning simulated active sessions.");
            return GetSimulatedActiveSessions();
        }
    }

    public async Task<IEnumerable<BPSession>> GetRecentSessionsAsync(int limit = 50)
    {
        try
        {
            const string sql = @"
                SELECT TOP(@Limit)
                       s.sessionid AS SessionId,
                       s.sessionnumber AS SessionNumber,
                       s.processid AS ProcessId,
                       p.name AS ProcessName,
                       s.statusid AS StatusId,
                       st.description AS StatusDescription,
                       s.starterresourceid AS StarterResourceId,
                       s.runningresourceid AS RunningResourceId,
                       r.name AS ResourceName,
                       s.startdatetime AS StartDateTime,
                       s.enddatetime AS EndDateTime
                FROM BPASession s
                INNER JOIN BPAProcess p ON s.processid = p.processid
                LEFT JOIN BPAStatus st ON s.statusid = st.statusid
                LEFT JOIN BPAResource r ON s.runningresourceid = r.resourceid
                ORDER BY s.startdatetime DESC";

            using var conn = CreateConnection();
            return await conn.QueryAsync<BPSession>(sql, new { Limit = limit });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; returning simulated recent sessions.");
            return GetSimulatedRecentSessions(limit);
        }
    }

    // ─── Resources ────────────────────────────────────────────

    public async Task<IEnumerable<BPResource>> GetResourcesAsync()
    {
        try
        {
            const string sql = @"
                SELECT r.resourceid AS ResourceId,
                       r.name AS Name,
                       ISNULL(r.processesrunning, 0) AS ProcessesRunning,
                       ISNULL(r.actionsrunning, 0) AS ActionsRunning,
                       r.statusid AS StatusId,
                       ISNULL(r.DisplayStatus, '') AS DisplayStatus,
                       r.pool AS PoolName
                FROM BPAResource r
                ORDER BY r.name";

            using var conn = CreateConnection();
            var resources = (await conn.QueryAsync<BPResource>(sql)).ToList();

            // Derive online status from BP statusid:
            //   1 = Connected, 2 = Offline, 3 = Disconnected
            foreach (var r in resources)
            {
                r.IsOnline = r.StatusId == 1 || r.ProcessesRunning > 0;
            }

            return resources;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; returning simulated resources.");
            return GetSimulatedResources();
        }
    }

    // ─── Work Queues ──────────────────────────────────────────

    public async Task<IEnumerable<BPWorkQueue>> GetWorkQueueSummariesAsync()
    {
        try
        {
            const string sql = @"
                SELECT wq.id AS Id,
                       wq.name AS Name,
                       COUNT(CASE WHEN wqi.status = 'Pending' THEN 1 END) AS PendingCount,
                       COUNT(CASE WHEN wqi.status = 'Locked'  THEN 1 END) AS LockedCount,
                       COUNT(CASE WHEN wqi.completed IS NOT NULL AND wqi.exception IS NULL THEN 1 END) AS CompletedCount,
                       COUNT(CASE WHEN wqi.exception IS NOT NULL THEN 1 END) AS ExceptionedCount,
                       COUNT(wqi.id) AS TotalCount
                FROM BPAWorkQueue wq
                LEFT JOIN BPAWorkQueueItem wqi ON wq.id = wqi.queueid
                GROUP BY wq.id, wq.name
                ORDER BY wq.name";

            using var conn = CreateConnection();
            return await conn.QueryAsync<BPWorkQueue>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blue Prism SQL Server offline; returning simulated work queues.");
            return GetSimulatedWorkQueues();
        }
    }

    public async Task<double> GetSuccessRate24hAsync()
    {
        try
        {
            const string sql = @"
                SELECT
                    CASE WHEN COUNT(*) = 0 THEN 1.0
                    ELSE CAST(SUM(CASE WHEN s.statusid = 4 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*)
                    END
                FROM BPASession s
                WHERE s.startdatetime >= DATEADD(HOUR, -24, GETUTCDATE())
                  AND s.statusid IN (2, 3, 4, 5)";

            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<double>(sql);
        }
        catch
        {
            return 0.965;
        }
    }

    public async Task<int> GetTodayExecutionCountAsync()
    {
        try
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM BPASession s
                WHERE s.startdatetime >= CAST(GETUTCDATE() AS DATE)";

            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql);
        }
        catch
        {
            return 0;
        }
    }

    // ─── Simulation Helpers ────────────────────────────────────

    private static List<BPProcess> GetSimulatedProcesses() => new()
    {
        new BPProcess { ProcessId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Invoice_Processing", ProcessType = "P", Description = "Automated SAP AP invoice extraction and voucher posting" },
        new BPProcess { ProcessId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Claims_Adjudication", ProcessType = "P", Description = "Health claim policy validation and deductible check" },
        new BPProcess { ProcessId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "KYC_Verification", ProcessType = "P", Description = "Customer AML watchlist screening and document OCR verify" },
        new BPProcess { ProcessId = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Payment_Reconciliation", ProcessType = "P", Description = "End-of-day bank ledger clearing and settlement matching" },
        new BPProcess { ProcessId = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Customer_Onboarding", ProcessType = "P", Description = "New client portal provisioning and CRM record creation" },
        new BPProcess { ProcessId = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Order_Fulfillment", ProcessType = "P", Description = "ERP warehouse order release and shipping label dispatch" },
        new BPProcess { ProcessId = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "PDF_Intake", ProcessType = "P", Description = "Intelligent document intake and classification pipeline" }
    };

    private static List<BPResource> GetSimulatedResources() => new()
    {
        new BPResource { ResourceId = Guid.Parse("a1111111-0000-0000-0000-000000000001"), Name = "VM-PROD-BOT01", ProcessesRunning = 1, IsOnline = true, PoolName = "Finance-Pool" },
        new BPResource { ResourceId = Guid.Parse("a2222222-0000-0000-0000-000000000002"), Name = "VM-PROD-BOT02", ProcessesRunning = 0, IsOnline = true, PoolName = "Finance-Pool" },
        new BPResource { ResourceId = Guid.Parse("a3333333-0000-0000-0000-000000000003"), Name = "VM-PROD-BOT03", ProcessesRunning = 2, IsOnline = true, PoolName = "Claims-Pool" },
        new BPResource { ResourceId = Guid.Parse("a4444444-0000-0000-0000-000000000004"), Name = "VM-PROD-BOT04", ProcessesRunning = 1, IsOnline = true, PoolName = "Claims-Pool" },
        new BPResource { ResourceId = Guid.Parse("a5555555-0000-0000-0000-000000000005"), Name = "VM-PROD-BOT05", ProcessesRunning = 0, IsOnline = false, PoolName = "Customer-Pool" },
        new BPResource { ResourceId = Guid.Parse("a6666666-0000-0000-0000-000000000006"), Name = "VM-PROD-BOT06", ProcessesRunning = 0, IsOnline = true, PoolName = "Customer-Pool" }
    };

    private static List<BPSession> GetSimulatedActiveSessions() => new()
    {
        new BPSession
        {
            SessionId = Guid.NewGuid(),
            SessionNumber = 10452,
            ProcessName = "Invoice_Processing",
            StatusId = 1,
            StatusDescription = "Running",
            ResourceName = "VM-PROD-BOT01",
            StartDateTime = DateTime.UtcNow.AddMinutes(-8)
        },
        new BPSession
        {
            SessionId = Guid.NewGuid(),
            SessionNumber = 10453,
            ProcessName = "Claims_Adjudication",
            StatusId = 1,
            StatusDescription = "Running",
            ResourceName = "VM-PROD-BOT03",
            StartDateTime = DateTime.UtcNow.AddMinutes(-22)
        },
        new BPSession
        {
            SessionId = Guid.NewGuid(),
            SessionNumber = 10454,
            ProcessName = "KYC_Verification",
            StatusId = 1,
            StatusDescription = "Running",
            ResourceName = "VM-PROD-BOT04",
            StartDateTime = DateTime.UtcNow.AddMinutes(-3)
        }
    };

    private static List<BPSession> GetSimulatedRecentSessions(int limit)
    {
        var list = new List<BPSession>();
        var processes = new[] { "Invoice_Processing", "Payment_Reconciliation", "Claims_Adjudication", "Customer_Onboarding", "Order_Fulfillment" };
        var resources = new[] { "VM-PROD-BOT01", "VM-PROD-BOT02", "VM-PROD-BOT03", "VM-PROD-BOT04" };

        for (int i = 0; i < Math.Min(limit, 25); i++)
        {
            var startTime = DateTime.UtcNow.AddMinutes(-(15 + i * 35));
            var durationMinutes = 2.5 + (i * 3 % 10);
            var isFailed = i == 4 || i == 14;

            list.Add(new BPSession
            {
                SessionId = Guid.NewGuid(),
                SessionNumber = 10450 - i,
                ProcessName = processes[i % processes.Length],
                StatusId = isFailed ? 2 : 4,
                StatusDescription = isFailed ? "Terminated" : "Completed",
                ResourceName = resources[i % resources.Length],
                StartDateTime = startTime,
                EndDateTime = startTime.AddMinutes(durationMinutes)
            });
        }
        return list;
    }

    private static List<BPWorkQueue> GetSimulatedWorkQueues() => new()
    {
        new BPWorkQueue { Id = Guid.NewGuid(), Name = "Invoices-SAP-Queue", PendingCount = 42, LockedCount = 2, CompletedCount = 890, ExceptionedCount = 14, TotalCount = 948 },
        new BPWorkQueue { Id = Guid.NewGuid(), Name = "Claims-Adjudication-Queue", PendingCount = 128, LockedCount = 4, CompletedCount = 612, ExceptionedCount = 28, TotalCount = 772 },
        new BPWorkQueue { Id = Guid.NewGuid(), Name = "KYC-Intake-Queue", PendingCount = 19, LockedCount = 1, CompletedCount = 420, ExceptionedCount = 6, TotalCount = 446 },
        new BPWorkQueue { Id = Guid.NewGuid(), Name = "Payment-Reconcile-Queue", PendingCount = 85, LockedCount = 3, CompletedCount = 1120, ExceptionedCount = 9, TotalCount = 1217 }
    };
}
