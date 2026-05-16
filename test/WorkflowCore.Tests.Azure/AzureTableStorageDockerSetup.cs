using System.Threading.Tasks;
using Testcontainers.Azurite;
using Xunit;

namespace WorkflowCore.Tests.Azure
{
    public class AzureTableStorageDockerSetup : IAsyncLifetime
    {
        private readonly AzuriteContainer _azuriteContainer;

        public static string ConnectionString { get; private set; }

        public AzureTableStorageDockerSetup()
        {
            _azuriteContainer = new AzuriteBuilder()
                .WithInMemoryPersistence()
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _azuriteContainer.StartAsync();
            ConnectionString = _azuriteContainer.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    [CollectionDefinition("AzureTableStorage collection")]
    public class AzureTableStorageCollection : ICollectionFixture<AzureTableStorageDockerSetup>
    {
    }
}