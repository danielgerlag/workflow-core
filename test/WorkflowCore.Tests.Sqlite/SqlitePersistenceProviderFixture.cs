using System;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Sqlite;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.Sqlite
{
    [Collection("Sqlite collection")]
    public class SqlitePersistenceProviderFixture : BasePersistenceFixture
    {
        public SqlitePersistenceProviderFixture(SqliteSetup setup)
        {
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                var db = new EntityFrameworkPersistenceProvider(new SqliteContextFactory(SqliteSetup.ConnectionString), true, false);
                db.EnsureStoreExists();
                return db;
            }
        }
    }
}
