using System.Threading.Tasks;
using Testcontainers.Oracle;
using Xunit;

namespace WorkflowCore.Tests.Oracle
{
    public class OracleDockerSetup : IAsyncLifetime
    {
        private readonly OracleContainer _oracleContainer;

        public static string ConnectionString { get; private set; }

        public OracleDockerSetup()
        {
            _oracleContainer = new OracleBuilder()
                .WithImage("gvenzl/oracle-free:latest")
                .WithUsername("TEST_WF")
                .WithPassword("test")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _oracleContainer.StartAsync();
            // Build connection string manually since TestContainers might not provide Oracle-specific format
            ConnectionString = $"Data Source=localhost:{_oracleContainer.GetMappedPublicPort(1521)}/FREEPDB1;User Id=TEST_WF;Password=test;";
        }

        public async Task DisposeAsync() => await _oracleContainer.DisposeAsync();
    }

    [CollectionDefinition("Oracle collection")]
    public class OracleCollection : ICollectionFixture<OracleDockerSetup> { }
}