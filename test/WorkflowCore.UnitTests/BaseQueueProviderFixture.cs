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

using Xunit;
using Xunit.Abstractions;

#endregion

namespace WorkflowCore.UnitTests
{
    public abstract class BaseQueueProviderFixture
    {
        protected IQueueProvider QueueProvider;
        protected ITestOutputHelper Console;
        private bool _stop;

        #region Setup

        protected void Setup()
        {
            while (QueueProvider.DequeueWork(QueueType.Event, CancellationToken.None).Result != null)
            {
                // Empty queue before test
            }

            while (QueueProvider.DequeueWork(QueueType.Workflow, CancellationToken.None).Result != null)
            {
                // Empty queue before test
            }
        }

        #endregion

        #region ShouldEnqueueAndDequeueAMessage

        [Fact]
        public void ShouldEnqueueAndDequeueAMessage()
        {
            var id = Guid.NewGuid().ToString();

            DoTest(id, QueueType.Event);

            id = Guid.NewGuid().ToString();
            DoTest(id, QueueType.Workflow);
        }

        private void DoTest(string id, QueueType queueType)
        {
            QueueProvider.QueueWork(id, queueType).Wait();
            var res = QueueProvider.DequeueWork(queueType, CancellationToken.None).Result;

            res.Should().Be(id);
        }

        #endregion

        #region ShouldEnqueueAndDequeueManyMessageOnManyThread

        [Fact]
        public void ShouldEnqueueAndDequeueManyMessageOnManyThread()
        {
            const int countEvent = 250;
            const int countThread = 10;
            const QueueType queueType = QueueType.Event;

            var guids = new ConcurrentDictionary<string, int>();

            _stop = false;
            var sw = Stopwatch.StartNew();

            var thDeque = StartDequeueTask(countThread, queueType, guids);
            var thEnque = StartEnqueueTask(countThread, countEvent, guids, queueType);

            Task.WaitAll(thEnque.ToArray());
            Console.WriteLine("Enqueue complete " + sw.ElapsedMilliseconds + " msec");

            _stop = true;
            Task.WaitAll(thDeque.ToArray());
            Console.WriteLine("Dequeue complete " + sw.ElapsedMilliseconds + " msec");

            foreach (var guid in guids)
            {
                guid.Value.Should().Be(1);
            }

            Console.WriteLine("Complete " + (guids.Count / (sw.ElapsedMilliseconds / 1000.0)) + " msg/sec");
        }

        private List<Task> StartEnqueueTask(int countThread, int countEvent, ConcurrentDictionary<string, int> guids, QueueType queueType)
        {
            Console.WriteLine("Start enqueue task");

            var thEnque = new List<Task>();
            for (int i = 0; i < countThread; i++)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("-> Enqueue task " + Task.CurrentId);

                    for (int j = 0; j < countEvent; j++)
                    {
                        var guid = Guid.NewGuid().ToString();
                        guids.TryAdd(guid, 0);
                        QueueProvider.QueueWork(guid, queueType).Wait();
                    }

                    Console.WriteLine("<- Enqueue task " + Task.CurrentId);
                });
                thEnque.Add(t);
            }

            return thEnque;
        }

        private List<Task> StartDequeueTask(int countThread, QueueType queueType, ConcurrentDictionary<string, int> guids)
        {
            Console.WriteLine("Start dequeue task");

            var thDeque = new List<Task>();
            for (int i = 0; i < countThread; i++)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("-> Dequeue task " + Task.CurrentId);
                    while (!_stop)
                    {
                        var id = QueueProvider.DequeueWork(queueType, CancellationToken.None).Result;
                        if (id != null) guids.AddOrUpdate(id, 0, (key, oldval) => oldval + 1);
                    }

                    Console.WriteLine("<- Dequeue task " + Task.CurrentId);
                });
                thDeque.Add(t);
            }

            return thDeque;
        }

        #endregion


    }
}