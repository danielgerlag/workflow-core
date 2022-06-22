using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.MySQL
{
    public class MysqlContext : WorkflowDbContext
    {
        private readonly string _connectionString;
        private readonly Action<MySqlDbContextOptionsBuilder> _mysqlOptionsAction;

        public MysqlContext(string connectionString, Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            _connectionString = connectionString;
            _mysqlOptionsAction = mysqlOptionsAction;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
#if NETSTANDARD2_0
            optionsBuilder.UseMySql(_connectionString, _mysqlOptionsAction);
#elif NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString), _mysqlOptionsAction);
#endif
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.ToTable("Subscription");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.ToTable("Workflow");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureExecutionPointerStorage(EntityTypeBuilder<PersistedExecutionPointer> builder)
        {
            builder.ToTable("ExecutionPointer");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureExecutionErrorStorage(EntityTypeBuilder<PersistedExecutionError> builder)
        {
            builder.ToTable("ExecutionError");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureExetensionAttributeStorage(EntityTypeBuilder<PersistedExtensionAttribute> builder)
        {
            builder.ToTable("ExtensionAttribute");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureEventStorage(EntityTypeBuilder<PersistedEvent> builder)
        {
            builder.ToTable("Event");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }

        protected override void ConfigureScheduledCommandStorage(EntityTypeBuilder<PersistedScheduledCommand> builder)
        {
            builder.ToTable("ScheduledCommand");
            builder.Property(x => x.PersistenceId).ValueGeneratedOnAdd();
        }
    }
}
