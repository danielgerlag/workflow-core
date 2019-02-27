using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Services.BackgroundTasks
{
    public class QueueConsumerScheduler : TaskScheduler, IDisposable
    {

        private readonly int _maxThreadCount;
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly List<Thread> _threads = new List<Thread>();

        public QueueConsumerScheduler(int maxThreads)
        {
            _maxThreadCount = maxThreads;

            for (int i = 0; i < maxThreads; i++)
            {
                var t = new Thread(new ThreadStart(RunTask));
                _threads.Add(t);
                t.Start();
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected override void QueueTask(Task task)
        {
            throw new NotImplementedException();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }

        private void RunTask()
        {
            foreach (var task in _tasks.GetConsumingEnumerable())
            {
                //Task.
            }
        }

        public void Dispose()
        {
            _tasks.CompleteAdding();
            foreach (var t in _threads)
                t.Join();
        }
    }
}
