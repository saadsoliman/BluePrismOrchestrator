using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BluePrismOrchestrator.Api.Models.Domain;
using System.Text.Json;

namespace BluePrismOrchestrator.Api.Data;

public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : base(options) { }

    public DbSet<OrchestratorSchedule> Schedules => Set<OrchestratorSchedule>();
    public DbSet<ProcessExecution> Executions => Set<ProcessExecution>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertHistory> AlertHistory => Set<AlertHistory>();
    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();
    public DbSet<ResourceUtilization> ResourceUtilizations => Set<ResourceUtilization>();
    public DbSet<ProcessMetrics> ProcessMetrics => Set<ProcessMetrics>();
    public DbSet<ScheduleOptimizationLog> ScheduleOptimizationLogs => Set<ScheduleOptimizationLog>();
    public DbSet<CutoffPolicy> CutoffPolicies => Set<CutoffPolicy>();
    public DbSet<CutoffEvent> CutoffEvents => Set<CutoffEvent>();
    public DbSet<BusinessOutputRule> BusinessOutputRules => Set<BusinessOutputRule>();
    public DbSet<BusinessOutputDeliveryLog> BusinessOutputDeliveryLogs => Set<BusinessOutputDeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // JSON list converters for complex properties stored as JSON
        var jsonOptions = new JsonSerializerOptions();

        var isNpgsql = Database.ProviderName?.Contains("Npgsql") == true;

        // OrchestratorSchedule
        modelBuilder.Entity<OrchestratorSchedule>(entity =>
        {
            entity.ToTable("schedules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessName);
            entity.HasIndex(e => e.IsEnabled);
            var prop = entity.Property(e => e.DependsOn)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, jsonOptions),
                      v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new());
            if (isNpgsql) prop.HasColumnType("jsonb");
        });

        // ProcessExecution
        modelBuilder.Entity<ProcessExecution>(entity =>
        {
            entity.ToTable("executions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.QueuedAt);
            entity.HasIndex(e => e.BPSessionId);
            entity.HasOne(e => e.Schedule).WithMany().HasForeignKey(e => e.ScheduleId);
        });

        // AlertRule
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.ToTable("alert_rules");
            entity.HasKey(e => e.Id);
            var chProp = entity.Property(e => e.Channels)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, jsonOptions),
                      v => JsonSerializer.Deserialize<List<NotificationChannel>>(v, jsonOptions) ?? new());
            if (isNpgsql) chProp.HasColumnType("jsonb");

            var emailProp = entity.Property(e => e.EmailRecipients)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, jsonOptions),
                      v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new());
            if (isNpgsql) emailProp.HasColumnType("jsonb");
        });

        // AlertHistory
        modelBuilder.Entity<AlertHistory>(entity =>
        {
            entity.ToTable("alert_history");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FiredAt);
            entity.HasIndex(e => e.Severity);
            entity.HasOne(e => e.AlertRule).WithMany().HasForeignKey(e => e.AlertRuleId);
            var sentProp = entity.Property(e => e.SentVia)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, jsonOptions),
                      v => JsonSerializer.Deserialize<List<NotificationChannel>>(v, jsonOptions) ?? new());
            if (isNpgsql) sentProp.HasColumnType("jsonb");
        });

        // AIRecommendation
        modelBuilder.Entity<AIRecommendation>(entity =>
        {
            entity.ToTable("ai_recommendations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ResourceUtilization
        modelBuilder.Entity<ResourceUtilization>(entity =>
        {
            entity.ToTable("resource_utilizations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ResourceName, e.Timestamp });
        });

        // ProcessMetrics
        modelBuilder.Entity<ProcessMetrics>(entity =>
        {
            entity.ToTable("process_metrics");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProcessName, e.PeriodStart });
        });

        // ScheduleOptimizationLog
        modelBuilder.Entity<ScheduleOptimizationLog>(entity =>
        {
            entity.ToTable("schedule_optimization_logs");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Schedule).WithMany().HasForeignKey(e => e.ScheduleId);
        });

        // CutoffPolicy
        modelBuilder.Entity<CutoffPolicy>(entity =>
        {
            entity.ToTable("cutoff_policies");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessName);
            entity.HasIndex(e => e.IsEnabled);
        });

        // CutoffEvent
        modelBuilder.Entity<CutoffEvent>(entity =>
        {
            entity.ToTable("cutoff_events");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CutoffPolicyId);
            entity.HasIndex(e => e.TriggerTime);
            entity.HasOne(e => e.Policy).WithMany().HasForeignKey(e => e.CutoffPolicyId);
        });

        // BusinessOutputRule
        modelBuilder.Entity<BusinessOutputRule>(entity =>
        {
            entity.ToTable("business_output_rules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessName);
            entity.HasIndex(e => e.IsEnabled);
        });

        // BusinessOutputDeliveryLog
        modelBuilder.Entity<BusinessOutputDeliveryLog>(entity =>
        {
            entity.ToTable("business_output_delivery_logs");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BusinessOutputRuleId);
            entity.HasOne(e => e.Rule).WithMany().HasForeignKey(e => e.BusinessOutputRuleId);
        });
    }
}
