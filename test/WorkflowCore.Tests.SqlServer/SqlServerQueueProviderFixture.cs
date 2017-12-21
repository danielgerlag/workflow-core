using System;
using System.Linq;

using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Services;

using Xunit;

namespace WorkflowCore.Tests.SqlServer {

    //[Collection("SqlServer collection")]
    public class SqlServerQueueProviderFixture : IDisposable
    {
        readonly SqlServerQueueProvider _qb;

        public SqlServerQueueProviderFixture(/*SqlDockerSetup setup*/) {
            var connectionString = "Server=(local);Database=wfc;User Id=wfc;Password=wfc;"; //SqlDockerSetup.ConnectionString;

            _qb = new SqlServerQueueProvider(connectionString, "UnitTest", true);
            _qb.Start().Wait();
        }

        public void Dispose()
        {
            _qb.Dispose();
        }

        [Fact]
        public void Test()
        {
            _qb.QueueWork("1", QueueType.Event);
        }


    }
}