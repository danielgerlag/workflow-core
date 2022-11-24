using System;
using System.Threading.Tasks;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    public class SqlDockerSetup : IAsyncLifetime
    {
        private readonly SqlServerResource _sqlServerResource;
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }

        public SqlDockerSetup()
        {
            _sqlServerResource = new SqlServerResource();
        }

        public async Task InitializeAsync()
        {
            await _sqlServerResource.InitializeAsync();
            ConnectionString = _sqlServerResource.CreateConnectionString("workflowcore-tests");
            ScenarioConnectionString = _sqlServerResource.CreateConnectionString("workflowcore-scenario-tests");
        }

        public Task DisposeAsync()
        {
            return _sqlServerResource.DisposeAsync();
        }
    }

    [CollectionDefinition("SqlServer collection")]
    public class SqlServerCollection : ICollectionFixture<SqlDockerSetup>
    {
    }
}