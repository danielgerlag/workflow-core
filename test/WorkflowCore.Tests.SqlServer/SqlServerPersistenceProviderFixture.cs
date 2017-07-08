using WorkflowCore.Interface;
using WorkflowCore.Persistence.SqlServer;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    [Collection("SqlServer collection")]
    public class SqlServerPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly string _connectionString;

        public SqlServerPersistenceProviderFixture(SqlDockerSetup setup)
        {
            _connectionString = SqlDockerSetup.ConnectionString;
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
