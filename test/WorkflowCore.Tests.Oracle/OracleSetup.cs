using System;
using System.Threading.Tasks;

using Xunit;

namespace WorkflowCore.Tests.Oracle
{
    public class OracleDockerSetup : IAsyncLifetime
    {
        public static string ConnectionString => "Data Source=(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521)) ) (CONNECT_DATA = (SERVICE_NAME = ORCLPDB1) ) );User ID=TEST_WF;Password=test;";

        public async Task InitializeAsync()
        {
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }

    [CollectionDefinition("Oracle collection")]
    public class OracleCollection : ICollectionFixture<OracleDockerSetup>
    {
    }
}