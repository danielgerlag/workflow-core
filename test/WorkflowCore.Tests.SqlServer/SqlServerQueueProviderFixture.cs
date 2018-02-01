#region using

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Services;

using Xunit;
using Xunit.Abstractions;

#endregion

namespace WorkflowCore.Tests.SqlServer
{
    [Collection("SqlServer collection")]
    public class SqlServerQueueProviderFixture : IDisposable
    {
        #region Init

        private readonly SqlServerQueueProvider _qb;
        private readonly ITestOutputHelper _console;

        public SqlServerQueueProviderFixture(ITestOutputHelper output, SqlDockerSetup setup)
        {
            _console = output;
            var connectionString = SqlDockerSetup.ConnectionString; 

            _qb = new SqlServerQueueProvider(connectionString, "UnitTest", true, true);
            _qb.Start().Wait();

            while (_qb.DequeueWork(QueueType.Event, CancellationToken.None).Result != null)
            {
                // Empty queue before test
            }

            while (_qb.DequeueWork(QueueType.Workflow, CancellationToken.None).Result != null)
            {
                // Empty queue before test
            }
        }

        public void Dispose()
        {
            _qb.Dispose();
        }

        #endregion

        #region QueueDeque

        [Fact]
        public void ShouldEnqueueAndDequeueAMessage()
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

        #endregion

        [Fact]
        public void ShouldEnqueueAndDequeueManyMessageOnManyThread()
        {
            const int countEvent = 250;
            const int countThread = 10;
            const QueueType queueType = QueueType.Event;

            var guids = new ConcurrentDictionary<string, int>();


            bool stop = false;
            var sw = Stopwatch.StartNew();

            _console.WriteLine("Start dequeue task");
            var thDeque = new List<Task>();
            for (int i = 0; i < countThread; i++)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    _console.WriteLine("-> Dequeue task " + Task.CurrentId);
                    while (!stop)
                    {
                        var id = _qb.DequeueWork(queueType, CancellationToken.None).Result;
                        if (id != null) guids.AddOrUpdate(id, 0, (key, oldval) => oldval + 1);
                    }

                    _console.WriteLine("<- Dequeue task " + Task.CurrentId);
                });
                thDeque.Add(t);
            }

            _console.WriteLine("Start enqueue task");
            var thEnque = new List<Task>();
            for (int i = 0; i < countThread; i++)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    _console.WriteLine("-> Enqueue task " + Task.CurrentId);

                    for (int j = 0; j < countEvent; j++)
                    {
                        var guid = Guid.NewGuid().ToString();
                        guids.TryAdd(guid, 0);
                        _qb.QueueWork(guid, queueType).Wait();
                    }

                    _console.WriteLine("<- Enqueue task " + Task.CurrentId);
                });
                thEnque.Add(t);
            }

            Task.WaitAll(thEnque.ToArray());
            _console.WriteLine("Enqueue complete " + sw.ElapsedMilliseconds + " msec");

            stop = true;
            Task.WaitAll(thDeque.ToArray());
            _console.WriteLine("Dequeue complete " + sw.ElapsedMilliseconds + " msec");

            foreach (var guid in guids)
            {
                guid.Value.Should().Be(1);
            }

            _console.WriteLine("MultiTest complete " + (guids.Count/(sw.ElapsedMilliseconds/1000.0)) + " msg/sec");
        }
    }
}