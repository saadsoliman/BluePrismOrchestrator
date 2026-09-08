# Blue Prism Smart Orchestrator User Guide

> **Audience:** RPA operations leads, Blue Prism administrators, Center of Excellence engineers, SREs, platform owners, and security reviewers responsible for operating the orchestrator after deployment.
>
> **Scope:** Day-2 operation of the web console, scheduling, alerting, AI recommendations, API recipes, security posture, backup, recovery, and production hardening. This guide does not replace Blue Prism product documentation.

---

## Table of Contents

1. [Product Overview](#1-product-overview)
2. [Architecture](#2-architecture)
3. [Environment Requirements](#3-environment-requirements)
4. [First-Time Setup](#4-first-time-setup)
5. [Web Console Guide](#5-web-console-guide)
6. [Common Operator Tasks](#6-common-operator-tasks)
7. [Alert Operations](#7-alert-operations)
8. [AI and ML Operations](#8-ai-and-ml-operations)
9. [Administration and Security](#9-administration-and-security)
10. [Backup, Recovery, and Continuity](#10-backup-recovery-and-continuity)
11. [Troubleshooting](#11-troubleshooting)
12. [API Reference and Recipes](#12-api-reference-and-recipes)
13. [Glossary](#13-glossary)
14. [Document Control](#14-document-control)

---

## 1. Product Overview

Blue Prism Smart Orchestrator is an operations layer for an existing Blue Prism Digital Workforce estate. It centralizes runtime visibility, schedule management, alerting, AI-assisted recommendations, and controlled `AutomateC.exe` actions behind a web console and REST API.

The orchestrator does not replace Blue Prism Control Room or modify Blue Prism process definitions. It reads operational data from the Blue Prism SQL Server database and can dispatch or stop sessions through AutomateC when explicitly requested by an operator, schedule, or accepted recommendation.

### Core Capabilities

| Capability | Operational Value |
|------------|-------------------|
| Real-time dashboard | View runtime resources, active sessions, work queues, KPIs, and recent alerts from one console. |
| SignalR updates | Push dashboard changes to connected browsers without manual reloads. |
| Workload-aware dispatch | Auto-select an online, low-utilization resource when no target resource is specified. |
| Smart schedules | Create cron-based orchestrator schedules and evaluate AI-managed schedules for better windows. |
| Alert rules | Configure threshold, anomaly, SLA, resource-down, and consecutive-failure alert rules. |
| AI recommendations | Review or dismiss recommendations for scheduling, capacity, failure prevention, and queue management. |
| Predictive views | Inspect failure heatmaps, capacity forecast, and anomaly feed. |
| API automation | Automate common operations through REST endpoints and PowerShell or curl. |

### Operating Principles

- Blue Prism remains the system of record for processes, queues, sessions, and runtime resources.
- The orchestrator database stores orchestrator-owned data such as schedules, alerts, metrics, recommendations, and Hangfire jobs.
- Production deployments must protect the API and Hangfire dashboard behind corporate authentication, network controls, or both.
- All service credentials must be stored outside source control.
- Operators should treat AI recommendations as decision support unless their organization has explicitly approved auto-application.

---

## 2. Architecture

```text
Browser / Operations Console
        |
        | HTTP, REST, SignalR
        v
React + TypeScript + Vite SPA
        |
        | /api/* and /hubs/*
        v
ASP.NET Core API + SignalR + Hangfire
        |                         |
        | read-only SQL           | AutomateC CLI
        v                         v
Blue Prism SQL Server DB     Blue Prism runtime host
        |
        | polling snapshots
        v
Orchestrator DB
PostgreSQL in production or in-memory for local development
```

### Components

| Component | Current Implementation |
|-----------|------------------------|
| Frontend | React 18, TypeScript, Vite, Recharts, SignalR client, lucide-react icons. |
| API | ASP.NET Core, controllers, SignalR hub, Swagger in development. |
| Background jobs | Hangfire with PostgreSQL storage when `UsePostgres=true`; memory storage otherwise. |
| Persistence | Entity Framework Core for orchestrator-owned data. |
| Blue Prism read path | SQL Server read access through `BluePrismDbReader`. |
| Blue Prism action path | `AutomateC.exe` via `AutomateCService`. |
| AI/ML | ML.NET predictive services and statistical anomaly detection. |

### Data Boundaries

| Boundary | Access Type | Notes |
|----------|-------------|-------|
| Blue Prism database | Read-only | Use a dedicated SQL login with `db_datareader` only. |
| Orchestrator database | Read/write | Stores alerts, schedules, metrics, recommendations, and Hangfire state. |
| AutomateC | Command execution | Used for run and graceful stop actions. Protect credentials and host access. |
| Notification channels | Outbound only | SMTP, Teams webhook, and generic webhook. |

---

## 3. Environment Requirements

### Runtime Requirements

| Component | Required Version | Notes |
|-----------|------------------|-------|
| .NET SDK | 10.0 or compatible preview/runtime for `net10.0` | The API project currently targets `net10.0`. Retarget the project before using an older SDK. |
| Node.js | 18 LTS or later | Node 20 LTS is recommended for enterprise developer workstations and CI. |
| npm | Bundled with Node.js | Use `npm ci` in CI and repeatable build environments. |
| PostgreSQL | 14 or later | Required for durable production persistence. |
| SQL Server access | Blue Prism database compatible | Read-only credentials are sufficient for the Blue Prism DB. |
| Blue Prism AutomateC | Blue Prism 7.x estate | Must be installed on the API host or a controlled runtime host where commands execute. |

### Network Requirements

| Flow | Default Local Endpoint | Production Guidance |
|------|------------------------|---------------------|
| Browser to frontend | `http://localhost:5173` | Serve from approved static hosting or reverse proxy. |
| Frontend to API | `/api/*` | Route through the same reverse proxy when possible. |
| Frontend to SignalR | `/hubs/dashboard` | WebSocket upgrade must be allowed. |
| API to Blue Prism DB | SQL Server port | Restrict to read-only service identity. |
| API to PostgreSQL | PostgreSQL port | Restrict to orchestrator service identity. |
| API to SMTP/webhooks | Outbound HTTPS/SMTP | Restrict destinations where possible. |

### Local Port Standard

The checked-in API launch profile and Vite proxy target `http://localhost:5043`. The frontend development server runs at `http://localhost:5173` and proxies `/api/*` and `/hubs/*` to the API.

For production, avoid split origins where possible. Place the SPA, API, and SignalR hub behind one reverse proxy origin.

---

## 4. First-Time Setup

### 4.1 Clone and Build

```powershell
git clone <your-repo-url> BluePrismOrchestrator
cd BluePrismOrchestrator
dotnet build
```

### 4.2 Install Frontend Dependencies

```powershell
cd src/frontend
npm ci
```

Use `npm install` for interactive development when intentionally changing dependencies. Use `npm ci` when the lock file should be authoritative.

### 4.3 Configure the API

Edit `src/BluePrismOrchestrator.Api/appsettings.json` for local development, or override the same keys through environment variables in production.

```jsonc
{
  "UsePostgres": true,
  "ConnectionStrings": {
    "OrchestratorDb": "Host=bp-orch-pg.internal;Database=BluePrismOrchestrator;Username=bp_orch_app;Password=...",
    "BluePrismDb": "Server=bp-sql.internal;Database=BluePrism;User Id=bp_orch_ro;Password=...;TrustServerCertificate=True;"
  },
  "AutomateC": {
    "ExecutablePath": "C:\\Program Files\\Blue Prism Limited\\Blue Prism Automate\\AutomateC.exe",
    "AuthMode": "SSO",
    "DbConnectionName": "BluePrism",
    "MaxConcurrentInvocations": 4,
    "TimeoutSeconds": 120
  },
  "BluePrismDb": {
    "ConnectionString": "Server=bp-sql.internal;Database=BluePrism;User Id=bp_orch_ro;Password=...;TrustServerCertificate=True;",
    "PollingIntervalSeconds": 10
  },
  "Alerts": {
    "Smtp": {
      "Host": "smtp.office365.com",
      "Port": 587,
      "Username": "orchestrator@company.com",
      "Password": "...",
      "FromAddress": "orchestrator@company.com",
      "UseSsl": true
    },
    "Teams": {
      "WebhookUrl": "https://outlook.office.com/webhook/..."
    },
    "Webhook": {
      "Url": "https://hooks.example.com/...",
      "Headers": {
        "X-Webhook-Secret": "..."
      }
    }
  }
}
```

### 4.4 Configuration Rules

- Never commit real credentials.
- Prefer environment variables, a vault, or platform secret injection for production.
- Use double underscores for environment variable nesting, for example `AutomateC__Password` and `Alerts__Smtp__Password`.
- Keep the Blue Prism SQL identity read-only.
- Keep the orchestrator database identity scoped to the orchestrator database only.
- Use `UsePostgres=true` for production. In-memory persistence is for local development and demonstrations only.

### 4.5 Run Locally

Terminal 1, API:

```powershell
cd src/BluePrismOrchestrator.Api
dotnet run
```

Terminal 2, frontend:

```powershell
cd src/frontend
npm run dev
```

Open `http://localhost:5173`.

The frontend development proxy expects the API at `http://localhost:5043`.

### 4.6 Production Build

```powershell
cd src/frontend
npm ci
npm run build
```

The static frontend output is written to `src/frontend/dist`. Serve it from IIS, nginx, a platform static host, or object storage/CDN, and route `/api/*` plus `/hubs/*` to the deployed API.

---

## 5. Web Console Guide

### 5.1 Navigation

The left sidebar provides the main work areas:

| Area | Purpose | Badge Meaning |
|------|---------|---------------|
| Live Operations | Runtime dashboard, session stream, queues, and recent alerts. | Current operational pressure. |
| AI Insights & ML | Recommendations, failure heatmap, capacity forecast, and anomaly feed. | Unresolved recommendations. |
| Analytics & SLA | Process trends, SLA compliance, and resource utilization heatmap. | None. |
| Smart Schedules | Orchestrator schedules and schedule optimization. | None. |
| Alerts & Rules | Alert rules and incident audit history. | Unacknowledged alerts. |

### 5.2 Header

| Control | Behavior |
|---------|----------|
| Live indicator | Shows SignalR connection status. Investigate if it stays disconnected. |
| Clock | Displays the current UTC-oriented operations time used for correlation. |
| Refresh | Re-fetches dashboard-relevant API data. |
| Dispatch Process | Opens the immediate process dispatch modal. |

### 5.3 Live Operations

Use this page during active monitoring and incident triage.

| Panel | What to Watch |
|-------|---------------|
| KPI cards | Online resources, active sessions, 24-hour success rate, queue backlog, daily executions, and process count. |
| Bot status grid | Offline resources, unexpectedly high utilization, and active session distribution. |
| Queue depth gauge | Pending, locked, completed, and exceptioned work queue counts. |
| Execution timeline | Running and recent sessions. Running sessions expose a graceful stop action. |
| Operational alerts | Recent alert history with quick acknowledge. |

### 5.4 AI Insights and ML

Use this page to evaluate machine-generated operational signals.

| Block | Purpose |
|-------|---------|
| Retrain ML Models | Starts background retraining for predictive services. |
| Recommendation cards | Shows AI recommendations with confidence and estimated impact. |
| Failure heatmap | Shows predicted process failure probability by hour. |
| Capacity forecast | Compares actual and predicted utilization. |
| Anomaly feed | Lists statistical outliers that may need correlation with incidents or deployments. |

Confidence bands:

| Confidence | Guidance |
|------------|----------|
| 90-100% | Strong signal. Auto-apply only if approved by policy. |
| 70-89% | Review and usually actionable. |
| 50-69% | Treat as informational. |
| Below 50% | Typically filtered out of the default recommendation list. |

### 5.5 Analytics and SLA

| View | Use Case |
|------|----------|
| Process performance trends | Compare duration, success rate, and throughput across time. |
| Enterprise SLA compliance | Review overall compliance, executions, breaches, and worst-performing processes. |
| Resource utilization heatmap | Identify overloaded resources, unused windows, and candidates for schedule movement. |

### 5.6 Smart Schedules

Schedules use five-part cron syntax:

```text
minute hour day-of-month month day-of-week
```

Example: `30 4 * * 1-5` runs at 04:30 Monday through Friday.

Schedule fields:

| Field | Required | Notes |
|-------|----------|-------|
| Name | Yes | Operator-facing schedule name. |
| Process | Yes | Must match a Blue Prism process name. |
| Cron Expression | Yes | Five-part cron expression. |
| Target Resource | No | Leave blank for workload-aware assignment. |
| Priority | Yes | Defaults to 5. Lower numbers should be treated as higher business priority by convention. |
| SLA Deadline | No | Used by SLA analytics and alerts. |
| Max Retries | No | Defaults to 3 in the API DTO. |
| AI Optimization Enabled | No | Allows schedule optimization recommendations. |
| Business Hours Only | No | Restricts execution to configured business hours. |
| Startup Parameters XML | No | Optional Blue Prism startup parameters. |
| Depends On | No | Optional dependency list for operational sequencing. |

### 5.7 Alerts and Rules

Rules support the following alert types:

| Type | Typical Use |
|------|-------------|
| Threshold | Queue depth, failure rate, duration, or other metric thresholds. |
| Anomaly | Statistical outliers from baseline behavior. |
| SLA Breach | Process or schedule fails SLA deadline. |
| Resource Down | Runtime resource is offline or not reporting. |
| Consecutive Failures | Repeated failed sessions for a process or resource. |

Notification channels:

- In-app alert history
- Email
- Teams webhook
- Generic webhook

Alert acknowledgements are operational acknowledgements. They do not delete the audit record.

---

## 6. Common Operator Tasks

### 6.1 Run a Process Immediately

1. Open **Live Operations**.
2. Click **Dispatch Process**.
3. Select the process.
4. Select a target resource, or leave it blank for auto-assignment.
5. Click **Dispatch Process**.
6. Confirm the returned execution ID and monitor the execution timeline.

### 6.2 Stop a Stuck Session

1. Open **Live Operations**.
2. Locate the running session in the execution timeline.
3. Click **Stop**.
4. Confirm the session closes in Blue Prism.
5. If the session remains running, escalate through the standard Blue Prism Control Room process.

The stop action requests a graceful stop through AutomateC. A process that is blocked inside an application interaction may not stop immediately.

### 6.3 Create a Schedule

1. Open **Smart Schedules**.
2. Click the add schedule control.
3. Enter name, process, cron expression, priority, and optional resource target.
4. Set AI optimization and business-hour options if required.
5. Save the schedule.
6. Verify that the schedule appears as enabled.

### 6.4 Optimize Schedules

1. Open **Smart Schedules**.
2. Trigger schedule optimization.
3. Review each recommendation, including original cron, suggested cron, reasoning, confidence, and estimated impact.
4. Apply only changes that fit business windows and upstream/downstream dependencies.
5. Monitor the next execution window.

### 6.5 Create an Alert Rule

1. Open **Alerts & Rules**.
2. Click **New Rule**.
3. Enter name, description, rule type, severity, scope, threshold fields, cooldown, and channels.
4. Save the rule.
5. Validate with a safe test condition in a non-production environment where possible.

### 6.6 Acknowledge an Alert

1. Open the dashboard alert stream or the alerts audit trail.
2. Review alert severity, message, process, resource, trigger value, and fired time.
3. Click **Ack** or **Acknowledge**.
4. Continue incident response outside the orchestrator if customer impact is suspected.

### 6.7 Retrain AI Models

Retrain when:

- A new Blue Prism process is onboarded.
- At least 50 new sessions have been recorded since the last training event.
- Schedules or business processing windows change materially.
- Forecast accuracy has drifted for multiple days.
- A quarterly baseline refresh is due.

Open **AI Insights & ML** and click **Retrain ML Models**. Treat the first few recommendation cycles after major process changes as supervised.

---

## 7. Alert Operations

### 7.1 Severity Matrix

| Severity | Response Target | Default Channels | Examples |
|----------|-----------------|------------------|----------|
| Critical | 15 minutes | In-app, Email, Teams, Webhook | Resource pool outage, severe backlog, repeated critical process failures. |
| Warning | 4 hours | In-app, Email | Rising failure rate, SLA watch list, queue backlog above warning threshold. |
| Info | Next business day | In-app | Informational anomaly, low-confidence recommendation, non-urgent signal. |

Adjust response targets to match your organization's incident policy.

### 7.2 Recommended Baseline Rules

| Rule | Suggested Starting Point | Severity |
|------|--------------------------|----------|
| High queue backlog | `PendingQueueItems > 200` for important queues. | Warning |
| Severe queue backlog | `PendingQueueItems > 500` for important queues. | Critical |
| Failure rate spike | `FailureRate > 15` over a 60-minute window. | Warning |
| Long execution duration | Process-specific threshold based on p95 runtime. | Warning |
| Resource down | Any production runtime resource offline for more than one polling cycle. | Critical |
| Consecutive failures | 3 consecutive failures for a critical process. | Critical |
| SLA breach | Process misses business deadline. | Warning or Critical |

### 7.3 On-Call Runbook

1. Acknowledge the alert in the orchestrator.
2. Identify affected process, resource, queue, trigger value, and fired time.
3. Check **AI Insights & ML** for correlated anomalies.
4. Check **Analytics & SLA** for recent trend changes.
5. Inspect Blue Prism Control Room when a session, resource, or queue requires direct Blue Prism action.
6. Stop only sessions that are safe to interrupt.
7. Record incident notes in the organization's incident tracker.
8. Tune the rule only after root cause or false-positive review.

### 7.4 Alert Hygiene

- Use cooldowns to prevent alert storms.
- Avoid routing all warnings to paging systems.
- Review unacknowledged alerts daily.
- Review rule thresholds after major process or volume changes.
- Keep alert names operationally meaningful, for example `Invoice queue backlog critical`.

---

## 8. AI and ML Operations

### 8.1 Model Governance

AI recommendations can influence production operations. Establish governance before allowing automated action:

| Control | Recommendation |
|---------|----------------|
| Ownership | Assign a named service owner for recommendation policy. |
| Approval | Require operator review unless explicit auto-apply approval exists. |
| Audit | Retain accept and dismiss history. |
| Drift review | Compare forecast and failure predictions against actual outcomes. |
| Change windows | Avoid applying schedule recommendations during freeze periods without approval. |

### 8.2 Failure Heatmap

The failure heatmap reports predicted failure probability by process and hour.

| Probability | Guidance |
|-------------|----------|
| Below 8% | Healthy. |
| 8-15% | Watch. |
| 15-25% | Avoid for non-critical dispatch where possible. |
| Above 25% | Investigate before adding load. |

### 8.3 Capacity Forecast

Capacity forecast compares actual utilization against predicted utilization and demand. Use it for:

- Scheduling non-critical work into lower utilization windows.
- Supporting capacity requests with trend evidence.
- Detecting demand shifts after business events or process changes.

### 8.4 Anomaly Feed

Anomaly detection highlights metrics that deviate from recent baseline behavior. Investigate anomalies alongside:

- Blue Prism releases and process changes.
- Windows patching or infrastructure maintenance.
- Source application outages.
- Business volume events.
- Queue input spikes.

### 8.5 Feedback Loop

Every accepted or dismissed recommendation should be treated as a governance signal. Dismissals are valuable because they prevent repeated recommendations that do not fit business context.

---

## 9. Administration and Security

### 9.1 Security Posture

The current API does not include built-in end-user authentication. Production deployments must add protection before exposing the service beyond a trusted private environment.

Minimum production controls:

- TLS termination at the edge.
- Corporate SSO through reverse proxy or ASP.NET Core authentication.
- Network restriction for API and database access.
- Protected Hangfire dashboard.
- Secret storage outside source control.
- Least-privilege database accounts.
- Centralized API logs.
- Change control for schedule and alert modifications.

### 9.2 Service Identities

| Identity | Permission Scope |
|----------|------------------|
| Blue Prism SQL login | `db_datareader` on the Blue Prism database only. |
| Orchestrator DB login | Read/write only on the orchestrator database. |
| AutomateC identity | Minimum rights required to run and stop approved processes. |
| SMTP identity | Send-only mailbox or app credential where possible. |
| Webhook secrets | Scoped to the target integration and rotated on personnel or vendor changes. |

### 9.3 Suggested Roles

| Role | Permissions |
|------|-------------|
| `rpa-viewer` | Read dashboards, alerts, analytics, and AI insights. |
| `rpa-operator` | Viewer plus dispatch, stop, and acknowledge. |
| `rpa-admin` | Operator plus manage schedules, rules, and AI retraining. |
| `rpa-platform` | Admin plus configuration, Hangfire, database operations, and production support. |

### 9.4 Adding SSO or RBAC

For application-layer authentication, add ASP.NET Core authentication and authorization before mapping protected controllers and hubs.

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
    options.ClientId = "<app-registration-id>";
    options.ClientSecret = "<from-vault>";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDispatch", p => p.RequireRole("rpa-operator", "rpa-admin"));
    options.AddPolicy("CanManageRules", p => p.RequireRole("rpa-admin"));
    options.AddPolicy("CanManageSchedules", p => p.RequireRole("rpa-admin"));
    options.AddPolicy("CanRetrainAI", p => p.RequireRole("rpa-admin"));
});
```

In the pipeline, call authentication before authorization:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Then apply `[Authorize]` or policy-specific authorization attributes to controllers and hubs.

### 9.5 Hangfire Dashboard Hardening

The current API enables `/hangfire` with a no-auth dashboard filter. Lock this down before production.

Recommended approach:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});

public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
            && http.User.IsInRole("rpa-platform");
    }
}
```

Alternative: expose Hangfire only on a private management listener or behind a VPN-only reverse proxy.

### 9.6 Secret Management

Recommended secret locations:

| Mechanism | Notes |
|-----------|-------|
| Environment variables | Simple and compatible with most hosts. |
| Azure Key Vault | Recommended for Azure deployments. |
| AWS Secrets Manager | Recommended for AWS deployments. |
| HashiCorp Vault | Recommended for centralized enterprise secret management. |
| Kubernetes secrets with external secret operator | Prefer backing with a cloud or enterprise vault. |

Rotation guidance:

| Secret | Recommended Rotation |
|--------|----------------------|
| Blue Prism SQL password | 90 days |
| Orchestrator DB password | 90 days |
| AutomateC native password | 90 days |
| SMTP password | 60-90 days |
| Teams or webhook URL | On owner change, suspected exposure, or integration change |
| OIDC client secret | 90 days or per enterprise policy |

### 9.7 Audit Logging

Enterprise deployments should capture:

| Event | Required Fields |
|-------|-----------------|
| Dispatch process | User, process, resource, parameters metadata, timestamp, source IP, result. |
| Stop session | User, session ID, process, resource, timestamp, source IP, result. |
| Create/update/delete schedule | User, schedule ID, before/after values, timestamp. |
| Create/update/delete alert rule | User, rule ID, before/after values, timestamp. |
| Acknowledge alert | User, alert ID, timestamp. |
| Retrain AI | User, timestamp, result, model metadata where available. |

The current data model records many operational timestamps. Add user identity and source IP fields before using the system for regulated production workflows.

### 9.8 Compliance Alignment

The orchestrator can support SOX, ISO 27001, SOC 2, and internal change-control programs when deployed with:

- Least-privilege service identities.
- SSO/RBAC enforcement.
- Centralized immutable logging.
- Backup and restore evidence.
- Reviewable alert and schedule changes.
- Controlled promotion of configuration and model artifacts.
- Documented incident and access review procedures.

---

## 10. Backup, Recovery, and Continuity

### 10.1 What to Back Up

| Component | Method | Frequency |
|-----------|--------|-----------|
| Orchestrator PostgreSQL database | `pg_dump` or managed database backup | Daily minimum |
| Hangfire schema | Included in orchestrator DB backup | Daily minimum |
| Alert rules and schedules | Included in orchestrator DB backup | Daily minimum |
| ML model artifacts | File-level backup of model artifact directory if persisted | Weekly or after retraining |
| Production configuration | Source control for non-secret config; vault backup for secrets | On change |
| Deployment manifests | Source control | On change |

### 10.2 Backup Example

```powershell
$env:PGPASSWORD = "<backup-password>"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

pg_dump `
  --host=bp-orch-pg.internal `
  --username=bp_orch_backup `
  --dbname=BluePrismOrchestrator `
  --format=custom `
  --file="C:\backups\bp-orch-$timestamp.dump"
```

### 10.3 Restore Example

```powershell
$env:PGPASSWORD = "<restore-password>"

pg_restore `
  --host=bp-orch-pg.internal `
  --username=bp_orch_admin `
  --dbname=BluePrismOrchestrator `
  --clean `
  --if-exists `
  "C:\backups\bp-orch-20260904-020000.dump"
```

### 10.4 Disaster Recovery Procedure

1. Provision a clean API host.
2. Restore the orchestrator database.
3. Restore model artifacts if they are persisted outside the database.
4. Reapply production configuration and secrets.
5. Confirm network access to Blue Prism SQL Server, PostgreSQL, and notification channels.
6. Start the API.
7. Start or route the frontend.
8. Validate `/api/dashboard/summary`, `/hubs/dashboard`, `/hangfire`, and a non-invasive read endpoint.
9. Confirm polling resumes and alerts are not storming.

Recommended targets:

| Metric | Target |
|--------|--------|
| RPO | 24 hours or better |
| RTO | 1 hour for standard deployment, lower for high-criticality operations |

---

## 11. Troubleshooting

### 11.1 Frontend Loads but API Calls Fail

Checks:

- Confirm whether the API is running on `5000` or `5043`.
- Confirm `src/frontend/vite.config.ts` proxy targets match the API port.
- Open `http://localhost:<api-port>/swagger` in development.
- Check browser console for failed `/api/*` or `/hubs/*` requests.

### 11.2 Sidebar Shows Reconnecting

Checks:

- Confirm `/hubs/dashboard/negotiate?negotiateVersion=1` returns successfully.
- Confirm reverse proxy allows WebSocket upgrade.
- Confirm CORS allows the frontend origin in local development.
- Confirm proxy idle timeout is greater than the SignalR reconnect interval.

### 11.3 Dashboard Shows Zero Resources

Checks:

- Confirm `BluePrismDb:ConnectionString` is set and reachable.
- Confirm the Blue Prism SQL login has read access to required tables.
- Review API logs for `BPPollingService` or SQL exceptions.
- Confirm the Blue Prism database contains active resources and sessions.

### 11.4 Dispatch Process Returns an Error

Checks:

- Confirm `AutomateC:ExecutablePath` points to a valid `AutomateC.exe`.
- Confirm `AutomateC:AuthMode`, `DbConnectionName`, `Username`, and `Password` are correct for the selected mode.
- Confirm the API host can execute AutomateC as the configured identity.
- Confirm the target process and resource names match Blue Prism exactly.
- Review API logs for AutomateC stderr.

### 11.5 AI Heatmap or Forecast Is Empty

Checks:

- Confirm model training has completed after startup.
- Confirm sufficient historical session data exists.
- Confirm process names are available from the Blue Prism database.
- Trigger **Retrain ML Models** after onboarding new processes.

### 11.6 Email Alerts Are Not Delivered

Checks:

- Confirm `Alerts:Smtp:Host`, `Port`, `Username`, `Password`, `FromAddress`, and `UseSsl`.
- Confirm firewall egress to the SMTP relay.
- Confirm SMTP relay allows the configured sender.
- Review `AlertService` errors in API logs.

### 11.7 Frontend Build Fails

```powershell
cd src/frontend
npm ci
npm run build
```

If dependency state is corrupted on a development workstation, remove `node_modules` and run `npm ci` again.

### 11.8 Hangfire Dashboard Is Unavailable

Checks:

- Confirm the API is running.
- Confirm `/hangfire` is routed through the reverse proxy if applicable.
- Confirm any dashboard authorization filter allows the current admin.
- Confirm Hangfire storage is reachable.

---

## 12. API Reference and Recipes

Default local API examples below use `http://localhost:5043`, matching the checked-in API launch profile and Vite proxy.

### 12.1 Conventions

PowerShell:

```powershell
$Base = "http://localhost:5043"
$Headers = @{ "Content-Type" = "application/json" }
```

Bash:

```bash
BASE_URL="http://localhost:5043"
```

All examples assume the current open local API. Add your organization's authentication headers or cookies after SSO/RBAC is enabled.

### 12.2 Endpoint Summary

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/dashboard/summary` | Dashboard aggregate snapshot. |
| GET | `/api/process` | List Blue Prism processes and metrics. |
| POST | `/api/process/{name}/run` | Dispatch a process. |
| GET | `/api/resource` | List runtime resources. |
| GET | `/api/resource/{name}/utilization?hours=24` | Resource utilization series. |
| GET | `/api/queue` | Work queue summary. |
| GET | `/api/schedule` | List schedules. |
| GET | `/api/schedule/{id}` | Get one schedule. |
| POST | `/api/schedule` | Create a schedule. |
| PUT | `/api/schedule/{id}` | Update a schedule. |
| DELETE | `/api/schedule/{id}` | Delete a schedule. |
| POST | `/api/schedule/optimize` | Generate schedule optimization results. |
| GET | `/api/alert/rules` | List alert rules. |
| POST | `/api/alert/rules` | Create alert rule. |
| PUT | `/api/alert/rules/{id}` | Update alert rule. |
| DELETE | `/api/alert/rules/{id}` | Delete alert rule. |
| GET | `/api/alert/history?limit=50` | List alert history. |
| POST | `/api/alert/history/{id}/acknowledge` | Acknowledge alert. |
| GET | `/api/analytics/trends?processName=...&days=7` | Process trend data. |
| GET | `/api/analytics/sla-compliance?days=7` | SLA compliance data. |
| GET | `/api/analytics/resource-heatmap` | Resource utilization heatmap. |
| GET | `/api/ai/recommendations` | List recommendations. |
| POST | `/api/ai/recommendations/{id}/accept` | Accept recommendation. |
| POST | `/api/ai/recommendations/{id}/dismiss` | Dismiss recommendation. |
| GET | `/api/ai/failure-heatmap` | Failure prediction grid. |
| GET | `/api/ai/capacity-forecast` | Capacity forecast. |
| GET | `/api/ai/anomalies?limit=20` | Recent anomalies. |
| POST | `/api/ai/retrain` | Start model retraining. |
| GET | `/api/session?activeOnly=true` | List active or recent sessions. |
| POST | `/api/session/{id}/stop?processName=...&resourceName=...` | Request graceful stop. |
| WS | `/hubs/dashboard` | SignalR dashboard hub. |
| GET | `/swagger` | Development API documentation. |
| GET | `/hangfire` | Hangfire dashboard; secure before production. |

### 12.3 Dashboard

```powershell
Invoke-RestMethod -Method Get -Uri "$Base/api/dashboard/summary"
```

```bash
curl -s "$BASE_URL/api/dashboard/summary"
```

### 12.4 Run a Process

```powershell
$body = @{
  targetResource = "BP-RES-01"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "$Base/api/process/Invoice_Processing/run" `
  -Headers $Headers `
  -Body $body
```

```bash
curl -s -X POST "$BASE_URL/api/process/Invoice_Processing/run" \
  -H "Content-Type: application/json" \
  -d '{"targetResource":"BP-RES-01"}'
```

Omit `targetResource` to let the workload balancer select a resource.

### 12.5 Create a Schedule

```powershell
$schedule = @{
  name                  = "Hourly Invoice Ingestion"
  processName           = "Invoice_Processing"
  cronExpression        = "0 * * * *"
  priority              = 5
  aiOptimizationEnabled = $true
  maxRetries            = 3
  businessHoursOnly     = $false
  isEnabled             = $true
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "$Base/api/schedule" `
  -Headers $Headers `
  -Body $schedule
```

### 12.6 Create an Alert Rule

```powershell
$rule = @{
  name              = "High Invoice Backlog"
  description       = "Triggered when the invoice queue exceeds 200 pending items"
  ruleType          = "Threshold"
  severity          = "Warning"
  processName       = "Invoice_Processing"
  metricName        = "PendingQueueItems"
  operator          = "GreaterThan"
  thresholdValue    = 200
  timeWindowMinutes = 60
  cooldownMinutes   = 15
  channels          = @("InApp", "Email")
  isEnabled         = $true
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "$Base/api/alert/rules" `
  -Headers $Headers `
  -Body $rule
```

### 12.7 Acknowledge Critical Alerts

```powershell
$open = Invoke-RestMethod -Method Get -Uri "$Base/api/alert/history?limit=200" |
  Where-Object { $_.severity -eq "Critical" -and -not $_.isAcknowledged }

foreach ($alert in $open) {
  Invoke-RestMethod -Method Post -Uri "$Base/api/alert/history/$($alert.id)/acknowledge"
}
```

### 12.8 Stop a Session

```powershell
$id = "<session-guid>"
$process = "Invoice_Processing"
$resource = "BP-RES-01"

Invoke-RestMethod -Method Post `
  -Uri "$Base/api/session/$id/stop?processName=$process&resourceName=$resource"
```

### 12.9 Pull Daily SLA Summary

```powershell
$today = Get-Date -Format "yyyyMMdd"
$sla = Invoke-RestMethod -Method Get -Uri "$Base/api/analytics/sla-compliance?days=1"

$sla | ConvertTo-Json -Depth 8 |
  Out-File "C:\reports\sla-$today.json"
```

### 12.10 SignalR Hub

The SignalR hub is intended for SignalR clients. Raw HTTP automation should use REST endpoints for snapshots.

Negotiate example:

```bash
curl -s -X POST "$BASE_URL/hubs/dashboard/negotiate?negotiateVersion=1"
```

Expected server-pushed events:

| Event | Payload | When |
|-------|---------|------|
| `SessionsUpdated` | `SessionDto[]` | Session state changes. |
| `ResourcesUpdated` | `ResourceStatusDto[]` | Resource state changes. |
| `QueuesUpdated` | `QueueSummaryDto[]` | Queue depth changes. |
| `AlertFired` | `AlertHistoryDto` | Alert rule fires. |
| `RecommendationsUpdated` | No payload | Recommendation set changes. |
| `AnomaliesUpdated` | `AnomalyDto[]` | Anomaly detection updates. |

---

## 13. Glossary

| Term | Definition |
|------|------------|
| AutomateC | Blue Prism command-line utility used to run and request-stop processes. |
| Blue Prism DB | SQL Server database used by Blue Prism as the operational system of record. |
| Confidence | Model-reported confidence for a recommendation or prediction. |
| Orchestrator DB | Database owned by this application for alerts, schedules, metrics, recommendations, and jobs. |
| Resource | Blue Prism runtime machine. |
| Schedule | Orchestrator-owned cron definition for process execution. |
| Session | One execution of a Blue Prism process on a resource. |
| SignalR | Real-time messaging channel used by the dashboard. |
| SLA | Service-level agreement or deadline used for operational compliance. |
| Work Queue | Blue Prism queue containing items awaiting processing or review. |

---

## 14. Document Control

| Field | Value |
|-------|-------|
| Document version | 2.0 |
| Product version covered | Blue Prism Smart Orchestrator 1.0.0 |
| Last reviewed | 2026-09-04 |
| Status | Enterprise operations guide |
| Owner | RPA Platform / Center of Excellence |

### Review Checklist

- Validate runtime versions against project files before each release.
- Validate local ports against `launchSettings.json` and `vite.config.ts`.
- Validate API endpoints against controller routes.
- Validate screenshots or UI labels when frontend navigation changes.
- Review security guidance after authentication, authorization, or hosting changes.
- Review backup and recovery procedures after persistence changes.
