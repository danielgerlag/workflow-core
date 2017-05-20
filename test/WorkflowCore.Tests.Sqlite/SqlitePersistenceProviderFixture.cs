using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.Sqlite;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.Sqlite
{
    [Collection("Sqlite collection")]
    public class SqlitePersistenceProviderFixture : BasePersistenceFixture
    {
        string _connectionString;

        public SqlitePersistenceProviderFixture(SqliteSetup setup)
        {
            _connectionString = setup.ConnectionString;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {                
                var db = new SqlitePersistenceProvider(_connectionString, true);
                db.EnsureStoreExists();
                return db;
            }
        }
    }
}
