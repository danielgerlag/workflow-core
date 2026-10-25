using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Services.BackgroundTasks;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class QueueConsumerScenario
    {
        [Fact]
        public async Task Completed_tasks_are_pruned_without_removing_in_flight_work()
        {
            using var queue = new SingleNodeQueueProvider();
            var consumer = new TestConsumer(queue);
            consumer.Start();
            try
            {
                await queue.QueueWork("in-flight", QueueType.Workflow);
                await consumer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

                for (var i = 0; i < 100; i++)
                    await queue.QueueWork(i.ToString(), QueueType.Workflow);
                await queue.QueueWork("marker", QueueType.Workflow);
                await consumer.MarkerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

                // The marker has entered ExecuteItem; wait until the dispatcher has tracked it.
                var deadline = DateTime.UtcNow.AddSeconds(5);
                Task[] retained;
                do
                {
                    retained = GetRunningTasks(consumer);
                    if (Array.FindAll(retained, t => !t.IsCompleted).Length == 2)
                        break;
                    await Task.Delay(10);
                } while (DateTime.UtcNow < deadline);

                Assert.Equal(2, Array.FindAll(retained, t => !t.IsCompleted).Length);
                Assert.InRange(retained.Length, 2, 3);
            }
            finally
            {
                consumer.Release.TrySetResult(true);
                consumer.Stop();
            }
        }

        private static Task[] GetRunningTasks(QueueConsumer consumer)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var taskLock = typeof(QueueConsumer).GetField("_runningTasksLock", flags).GetValue(consumer);
            lock (taskLock)
                return ((List<Task>)typeof(QueueConsumer).GetField("_runningTasks", flags).GetValue(consumer)).ToArray();
        }

        private class TestConsumer : QueueConsumer
        {
            public readonly TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> MarkerStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public TestConsumer(IQueueProvider queue)
                : base(queue, NullLoggerFactory.Instance, new WorkflowOptions(new ServiceCollection()))
            {
            }

            protected override QueueType Queue => QueueType.Workflow;
            protected override int MaxConcurrentItems => 3;

            protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
            {
                if (itemId == "in-flight" || itemId == "marker")
                {
                    (itemId == "in-flight" ? Started : MarkerStarted).TrySetResult(true);
                    await Release.Task;
                }
            }
        }
    }
}
