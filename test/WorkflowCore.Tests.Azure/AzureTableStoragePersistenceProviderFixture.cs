using Azure.Data.Tables;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Azure.Services;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.Azure
{
    [Collection("AzureTableStorage collection")]
    public class AzureTableStoragePersistenceProviderFixture : BasePersistenceFixture
    {
        private IPersistenceProvider _subject;

        public AzureTableStoragePersistenceProviderFixture(AzureTableStorageDockerSetup dockerSetup)
        {
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                if (_subject == null)
                {
                    var tableServiceClient = new TableServiceClient(AzureTableStorageDockerSetup.ConnectionString);
                    var provider = new AzureTableStoragePersistenceProvider(tableServiceClient, "TestWorkflowCore");
                    provider.EnsureStoreExists();
                    _subject = provider;
                }
                return _subject;
            }
        }
    }
}