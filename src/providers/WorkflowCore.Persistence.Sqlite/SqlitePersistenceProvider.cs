using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.Sqlite
{
    public class SqlitePersistenceProvider : EntityFrameworkPersistenceProvider
    {
        private readonly string _connectionString;

        public SqlitePersistenceProvider(string connectionString, bool canCreateDB)
            : base(canCreateDB, false)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.ForSqliteToTable("Subscription");            
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.ForSqliteToTable("Workflow");
        }
        
        protected override void ConfigurePublicationStorage(EntityTypeBuilder<PersistedPublication> builder)
        {
            builder.ForSqliteToTable("UnpublishedEvent");
        }
        
    }
}
