using System;
using System.Threading.Tasks;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL
{
    public class PostgresDockerSetup : IAsyncLifetime
    {
        private readonly PostgreSqlResource _postgreSqlResource;
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }

        public PostgresDockerSetup()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            _postgreSqlResource = new PostgreSqlResource();
        }

        public async Task InitializeAsync()
        {
            await _postgreSqlResource.InitializeAsync();
            ConnectionString = _postgreSqlResource.ConnectionString;
            ScenarioConnectionString = _postgreSqlResource.ConnectionString;
        }

        public Task DisposeAsync()
        {
            return _postgreSqlResource.DisposeAsync();
        }
    }

    [CollectionDefinition("Postgres collection")]
    public class PostgresCollection : ICollectionFixture<PostgresDockerSetup>
    {
    }
}