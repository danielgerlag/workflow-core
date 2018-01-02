#region using

using System;
using System.Linq;
using System.Threading;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Services;

using Xunit;

#endregion

namespace WorkflowCore.Tests.SqlServer
{
    //[Collection("SqlServer collection")]
    public class SqlServerQueueProviderFixture : IDisposable
    {
        readonly SqlServerQueueProvider _qb;

        public SqlServerQueueProviderFixture( /*SqlDockerSetup setup*/ )
        {
            var connectionString = "Server=(local);Database=wfc;User Id=wfc;Password=wfc;"; //SqlDockerSetup.ConnectionString;

            _qb = new SqlServerQueueProvider(connectionString, "UnitTest", true);
            _qb.Start().Wait();

            while (_qb.DequeueWork(QueueType.Event, CancellationToken.None).Result != null) { }
            while (_qb.DequeueWork(QueueType.Workflow, CancellationToken.None).Result != null) { }
        }

        public void Dispose()
        {
            _qb.Dispose();
        }

        [Fact]
        public void Test()
        {
            var id = Guid.NewGuid().ToString();

            DoTest(id, QueueType.Event);

            id = Guid.NewGuid().ToString();
            DoTest(id, QueueType.Workflow);
        }

        void DoTest(string id, QueueType queueType)
        {
            _qb.QueueWork(id, queueType).Wait();
            var res = _qb.DequeueWork(queueType, CancellationToken.None).Result;

            res.Should().Be(id);
        }
    }
}