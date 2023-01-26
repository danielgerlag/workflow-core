using System;
using System.Threading;
using WorkflowCore.Interface;

namespace WorkflowCore.Services.BackgroundTasks.RunnablePoller
{
    internal abstract class BaseWorkflowRunnablePoller : IBackgroundTask
    {
        private readonly TimeSpan _pollInterval;
        private Timer _pollTimer;

        protected BaseWorkflowRunnablePoller(TimeSpan pollInterval)
        {
            _pollInterval = pollInterval;
        }

        public void Start()
        {
            _pollTimer = new Timer(new TimerCallback(PollRunnables), null, TimeSpan.FromSeconds(0), _pollInterval);
        }

        public void Stop()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }


        protected abstract void PollRunnables(object target);
    }
}
