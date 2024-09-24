using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.SqlServer;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public class SqlServerOptimizedPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly string _connectionString;

        public SqlServerOptimizedPersistenceProviderFixture(SqlDockerSetup setup)
        {
            _connectionString = SqlDockerSetup.ConnectionString;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                var db = new LargeDataOptimizedEntityFrameworkPersistenceProvider(new SqlContextFactory(_connectionString), true, true);
                db.EnsureStoreExists();
                return db;
            }
        }
    }
}