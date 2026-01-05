using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.PostgreSQL
{
    public class PostgresContext : WorkflowDbContext
    {
        private readonly string _connectionString;
        private readonly string _schemaName;

        public PostgresContext(string connectionString,string schemaName)
            :base()
        {   
            _connectionString = connectionString;
            _schemaName = schemaName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql(_connectionString);
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.ToTable("Subscription", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.ToTable("Workflow", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }
                
        protected override void ConfigureExecutionPointerStorage(EntityTypeBuilder<PersistedExecutionPointer> builder)
        {
            builder.ToTable("ExecutionPointer", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureExecutionErrorStorage(EntityTypeBuilder<PersistedExecutionError> builder)
        {
            builder.ToTable("ExecutionError", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureExetensionAttributeStorage(EntityTypeBuilder<PersistedExtensionAttribute> builder)
        {
            builder.ToTable("ExtensionAttribute", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureEventStorage(EntityTypeBuilder<PersistedEvent> builder)
        {
            builder.ToTable("Event", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureScheduledCommandStorage(EntityTypeBuilder<PersistedScheduledCommand> builder)
        {
            builder.ToTable("ScheduledCommand", _schemaName);
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PersistedWorkflow>(x =>
            {
                x.Property(p => p.CompleteTime)
                 .HasColumnType("timestamp with time zone");

                x.Property(p => p.CreateTime)
                 .HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<PersistedExecutionPointer>(x =>
            {
                x.Property(p => p.SleepUntil)
                 .HasColumnType("timestamp with time zone");

                x.Property(p => p.StartTime)
                 .HasColumnType("timestamp with time zone");

                x.Property(p => p.EndTime)
                 .HasColumnType("timestamp with time zone");

            });

            modelBuilder.Entity<PersistedExecutionError>(x =>
            {
                x.Property(p => p.ErrorTime)
                 .HasColumnType("timestamp with time zone");

            });

            modelBuilder.Entity<PersistedSubscription>(x =>
            {
                x.Property(p => p.SubscribeAsOf)
                 .HasColumnType("timestamp with time zone");

                x.Property(p => p.ExternalTokenExpiry)
                 .HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<PersistedEvent>(
                x => x.Property(x => x.EventTime)
                .HasColumnType("timestamp with time zone")
            );
        }
    }
}

