using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.PostgreSQL
{
    public class PostgresPersistenceProvider : EntityFrameworkPersistenceProvider
    {
        private readonly string _connectionString;

        public PostgresPersistenceProvider(string connectionString)
        {   
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql(_connectionString);
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.Property(x => x.ClusterKey).ValueGeneratedOnAdd();
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.Property(x => x.ClusterKey).ValueGeneratedOnAdd();
        }
    }
}

