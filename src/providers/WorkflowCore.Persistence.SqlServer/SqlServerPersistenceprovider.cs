using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.SqlServer
{
    public class SqlServerPersistenceProvider : EntityFrameworkPersistenceProvider
    {
        private readonly string _connectionString;

        public SqlServerPersistenceProvider(string connectionString, bool canCreateDB, bool canMigrateDB)
            : base(canCreateDB, canMigrateDB)
        {
            if (!connectionString.Contains("MultipleActiveResultSets"))
                connectionString += ";MultipleActiveResultSets=True";

            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.ForSqlServerToTable("Subscription", "wfc");
            builder.Property(x => x.ClusterKey).UseSqlServerIdentityColumn();
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.ForSqlServerToTable("Workflow", "wfc");
            builder.Property(x => x.ClusterKey).UseSqlServerIdentityColumn();
        }
        
        protected override void ConfigurePublicationStorage(EntityTypeBuilder<PersistedPublication> builder)
        {
            builder.ForSqlServerToTable("UnpublishedEvent", "wfc");
            builder.Property(x => x.ClusterKey).UseSqlServerIdentityColumn();
        }
                
    }
}
