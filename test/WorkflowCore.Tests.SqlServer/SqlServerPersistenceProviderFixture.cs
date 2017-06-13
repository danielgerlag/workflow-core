using WorkflowCore.Interface;
using WorkflowCore.Persistence.SqlServer;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    [Collection("SqlServer collection")]
    public class SqlServerPersistenceProviderFixture : BasePersistenceFixture
    {
        string _connectionString;

        public SqlServerPersistenceProviderFixture(SqlServerSetup setup)
        {
            _connectionString = setup.ConnectionString;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                var db = new SqlServerPersistenceProvider(_connectionString, true, true);
                db.EnsureStoreExists();
                return db;
            }
        }
    }
}
