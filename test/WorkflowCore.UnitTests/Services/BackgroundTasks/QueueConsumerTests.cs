using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.BackgroundTasks;
using Xunit;

namespace WorkflowCore.UnitTests.Services.BackgroundTasks
{
    public class QueueConsumerTests
    {
        [Fact(DisplayName = "ExecuteItem should clear secondPasses after processing completes")]
        public async Task ExecuteItem_ShouldClearSecondPasses_AfterProcessing()
        {
            var queue = new ScriptedQueueProvider();
            var options = new WorkflowOptions(A.Fake<IServiceCollection>());
            options.UseIdleTime(TimeSpan.FromMilliseconds(20));

            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new TestQueueConsumer(queue, new LoggerFactory(), options)
            {
                OnProcess = async (id, ct) =>
                {
                    entered.TrySetResult(true);
                    await release.Task.ConfigureAwait(false);
                }
            };

            try
            {
                consumer.Start();
                queue.Enqueue("wf-1");

                await WaitUntil(() => entered.Task.IsCompleted, TimeSpan.FromSeconds(5));

                queue.Enqueue("wf-1");
                await WaitUntil(() => consumer.HasSecondPass("wf-1"), TimeSpan.FromSeconds(5));
                consumer.HasSecondPass("wf-1").Should().BeTrue();

                release.TrySetResult(true);

                await WaitUntil(() => !consumer.HasActiveTask("wf-1") && !consumer.HasSecondPass("wf-1"), TimeSpan.FromSeconds(5));
                consumer.HasSecondPass("wf-1").Should().BeFalse();
            }
            finally
            {
                release.TrySetResult(true);
                consumer.Stop();
            }
        }

        private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (!condition())
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Condition was not met within the allotted time.");
                }

                await Task.Delay(20);
            }
        }

        private class TestQueueConsumer : QueueConsumer
        {
            public TestQueueConsumer(IQueueProvider queueProvider, ILoggerFactory loggerFactory, WorkflowOptions options)
                : base(queueProvider, loggerFactory, options)
            {
            }

            public Func<string, CancellationToken, Task> OnProcess { get; set; }

            protected override QueueType Queue => QueueType.Workflow;

            protected override int MaxConcurrentItems => 4;

            protected override Task ProcessItem(string itemId, CancellationToken cancellationToken)
            {
                return OnProcess != null ? OnProcess(itemId, cancellationToken) : Task.CompletedTask;
            }

            public bool HasSecondPass(string itemId)
            {
                var field = typeof(QueueConsumer).GetField("_secondPasses", BindingFlags.NonPublic | BindingFlags.Instance);
                var set = field.GetValue(this);
                return (bool)set.GetType().GetMethod("Contains", new[] { typeof(string) }).Invoke(set, new object[] { itemId });
            }

            public bool HasActiveTask(string itemId)
            {
                var field = typeof(QueueConsumer).GetField("_activeTasks", BindingFlags.NonPublic | BindingFlags.Instance);
                var dict = field.GetValue(this);
                lock (dict)
                {
                    return (bool)dict.GetType().GetMethod("ContainsKey").Invoke(dict, new object[] { itemId });
                }
            }
        }

        private class ScriptedQueueProvider : IQueueProvider
        {
            private readonly ConcurrentQueue<string> _items = new ConcurrentQueue<string>();

            public bool IsDequeueBlocking => false;

            public void Enqueue(string id) => _items.Enqueue(id);

            public Task QueueWork(string id, QueueType queue)
            {
                return Task.CompletedTask;
            }

            public Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
            {
                if (_items.TryDequeue(out var id))
                {
                    return Task.FromResult(id);
                }

                return Task.FromResult<string>(null);
            }

            public Task Start() => Task.CompletedTask;

            public Task Stop() => Task.CompletedTask;

            public void Dispose()
            {
            }
        }
    }
}
