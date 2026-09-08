using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Configuration;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Services;
using BluePrismOrchestrator.Api.Services.AI;

var builder = WebApplication.CreateBuilder(args);

// ─── Options Configuration ─────────────────────────────────────
builder.Services.Configure<AutomateCOptions>(builder.Configuration.GetSection("AutomateC"));
builder.Services.Configure<BluePrismDbOptions>(builder.Configuration.GetSection("BluePrismDb"));
builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection("Alerts"));
builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection("LLM"));

// ─── Database Registration ─────────────────────────────────────
var pgConn = builder.Configuration.GetConnectionString("OrchestratorDb");
var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres", false);

if (usePostgres && !string.IsNullOrWhiteSpace(pgConn))
{
    builder.Services.AddDbContext<OrchestratorDbContext>(opt => opt.UseNpgsql(pgConn));
}
else
{
    builder.Services.AddDbContext<OrchestratorDbContext>(opt =>
        opt.UseInMemoryDatabase("BluePrismOrchestrator"));
}

// ─── Hangfire Job Scheduling ───────────────────────────────────
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings();

    if (usePostgres && !string.IsNullOrWhiteSpace(pgConn))
    {
        config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(pgConn));
    }
    else
    {
        config.UseMemoryStorage();
    }
});
builder.Services.AddHangfireServer();

// ─── Core & AI Services ─────────────────────────────────────────
builder.Services.AddScoped<BluePrismDbReader>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAutomateCService, AutomateCService>();
builder.Services.AddScoped<IWorkloadBalancerService, WorkloadBalancerService>();
builder.Services.AddScoped<ISmartSchedulerService, SmartSchedulerService>();
builder.Services.AddSingleton<IPredictiveAnalyticsService, PredictiveAnalyticsService>();
builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
builder.Services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ICutoffService, CutoffService>();
builder.Services.AddScoped<IBusinessOutputService, BusinessOutputService>();

// ─── Background Polling Service ────────────────────────────────
builder.Services.AddHostedService<BPPollingService>();

// ─── SignalR Real-Time Hub ─────────────────────────────────────
builder.Services.AddSignalR();

// ─── Controllers & Swagger ─────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Blue Prism Smart Orchestrator API",
        Version = "v1",
        Description = "Intelligent AI orchestration layer for Blue Prism RPA"
    });
});

// ─── CORS Policy ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ─── Initialize DB & Seed Data ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    await DbInitializer.InitializeAsync(db);

    // Initial training of predictive analytics
    var analytics = scope.ServiceProvider.GetRequiredService<IPredictiveAnalyticsService>();
    _ = Task.Run(() => analytics.TrainModelsAsync());
}

// ─── HTTP Pipeline ─────────────────────────────────────────────
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BP Smart Orchestrator v1"));
}

app.UseRouting();

// Hangfire Dashboard for visual job tracking
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireNoAuthFilter() }
});

// ─── Recurring Hangfire Jobs ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurring.AddOrUpdate<IAlertService>(
        "alert-rule-evaluation",
        svc => svc.EvaluateAlertRulesAsync(),
        "*/1 * * * *");

    recurring.AddOrUpdate<IAnomalyDetectionService>(
        "anomaly-detection",
        svc => svc.DetectAnomaliesAsync(),
        "*/2 * * * *");

    recurring.AddOrUpdate<ISmartSchedulerService>(
        "schedule-sync",
        svc => svc.SyncAllSchedulesAsync(),
        "0 * * * *");

    recurring.AddOrUpdate<ICutoffService>(
        "cutoff-enforcement",
        svc => svc.EvaluateCutoffsAsync(),
        "*/2 * * * *");

    recurring.AddOrUpdate<IBusinessOutputService>(
        "output-delivery",
        svc => svc.DeliverOutputsAsync(),
        "*/5 * * * *");
}

app.MapControllers();
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();

// Local Hangfire Dashboard Auth helper
public class HangfireNoAuthFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
