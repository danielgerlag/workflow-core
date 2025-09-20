using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework.Models;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public abstract class WorkflowDbContext : DbContext
    {
        protected abstract void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder);
        protected abstract void ConfigureExecutionPointerStorage(EntityTypeBuilder<PersistedExecutionPointer> builder);
        protected abstract void ConfigureExecutionErrorStorage(EntityTypeBuilder<PersistedExecutionError> builder);
        protected abstract void ConfigureExetensionAttributeStorage(EntityTypeBuilder<PersistedExtensionAttribute> builder);
        protected abstract void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder);
        protected abstract void ConfigureEventStorage(EntityTypeBuilder<PersistedEvent> builder);
        protected abstract void ConfigureScheduledCommandStorage(EntityTypeBuilder<PersistedScheduledCommand> builder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var workflows = modelBuilder.Entity<PersistedWorkflow>();
            workflows.HasIndex(x => x.InstanceId).IsUnique();
            workflows.HasIndex(x => x.NextExecution);

            var executionPointers = modelBuilder.Entity<PersistedExecutionPointer>();
            var executionErrors = modelBuilder.Entity<PersistedExecutionError>();
            var extensionAttributes = modelBuilder.Entity<PersistedExtensionAttribute>();

            var subscriptions = modelBuilder.Entity<PersistedSubscription>();
            subscriptions.HasIndex(x => x.SubscriptionId).IsUnique();
            subscriptions.HasIndex(x => x.EventName);
            subscriptions.HasIndex(x => x.EventKey);

            var events = modelBuilder.Entity<PersistedEvent>();
            events.HasIndex(x => x.EventId).IsUnique();
            events.HasIndex(x => new { x.EventName, x.EventKey });
            events.HasIndex(x => x.EventTime);
            events.HasIndex(x => x.IsProcessed);

            var commands = modelBuilder.Entity<PersistedScheduledCommand>();
            commands.HasIndex(x => x.ExecuteTime);
            commands.HasIndex(x => new { x.CommandName, x.Data}).IsUnique();

            ConfigureWorkflowStorage(workflows);
            ConfigureExecutionPointerStorage(executionPointers);
            ConfigureExecutionErrorStorage(executionErrors);
            ConfigureExetensionAttributeStorage(extensionAttributes);
            ConfigureSubscriptionStorage(subscriptions);
            ConfigureEventStorage(events);
            ConfigureScheduledCommandStorage(commands);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }
}
