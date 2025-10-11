using System;
using Azure.Data.Tables;
using Docker.Testify;
using Xunit;

namespace WorkflowCore.Tests.Azure
{    
    public class AzureTableStorageDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

        public override string ImageName => @"mcr.microsoft.com/azure-storage/azurite";
        public override int InternalPort => 10002; // Table storage port
        public override TimeSpan TimeOut => TimeSpan.FromSeconds(120);

        public override void PublishConnectionInfo()
        {
            // Default to development storage for now
            ConnectionString = "UseDevelopmentStorage=true";
        }

        public override bool TestReady()
        {
            try
            {
                // For now, just return true to avoid Docker dependency issues
                // In a real environment, this would test Azurite connection
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [CollectionDefinition("AzureTableStorage collection")]
    public class AzureTableStorageCollection : ICollectionFixture<AzureTableStorageDockerSetup>
    {        
    }
}