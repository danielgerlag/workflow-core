using Xunit;
using System;
using System.Threading.Tasks;
using Squadron;

namespace WorkflowCore.Tests.MySQL
{
    public class MysqlDockerSetup : IAsyncLifetime
    {
        private readonly MySqlResource _mySqlResource;
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }

        public MysqlDockerSetup()
        {
            _mySqlResource = new MySqlResource();
        }

        public async Task InitializeAsync()
        {
            await _mySqlResource.InitializeAsync();
            var workflowConnection = await _mySqlResource.CreateDatabaseAsync("workflow");
            ConnectionString = workflowConnection.ConnectionString;
            var scenariosConnection = await _mySqlResource.CreateDatabaseAsync("scenarios");
            ScenarioConnectionString = scenariosConnection.ConnectionString;
        }

        public Task DisposeAsync()
        {
            return _mySqlResource.DisposeAsync();
        }
    }
    
    [CollectionDefinition("Mysql collection")]
    public class MysqlCollection : ICollectionFixture<MysqlDockerSetup>
    {
    }
}