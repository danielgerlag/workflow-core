using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <summary>
    /// Single node in-memory implementation of IConcurrencyProvider
    /// </summary>
    public class SingleNodeConcurrencyProvider : IConcurrencyProvider
    {
                
        private ConcurrentQueue<string> _runQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<EventPublication> _publishQueue = new ConcurrentQueue<EventPublication>();
        private ConcurrentQueue<EventPublication> _deferredPublishQueue = new ConcurrentQueue<EventPublication>();
        private List<string> _locks = new List<string>();
        private Timer _flushTimer;

        public void StartupNode()
        {
            //todo: read persisted publish queue from disk
            _flushTimer = new Timer(new TimerCallback(FlushDeferredPublications), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public void ShutdownNode()
        {
            _flushTimer.Dispose();
            FlushDeferredPublications(null);
            //todo: persist publish queue to disk            
        }

        public async Task EnqueueForProcessing(string Id)
        {
            _runQueue.Enqueue(Id);
        }

        public async Task<string> DequeueForProcessing()
        {            
            string id;
            if (_runQueue.TryDequeue(out id))
            {
                return id;
            }
            return null;
        }

        public async Task<bool> AcquireLock(string Id)
        {
            lock (_locks)
            {
                if (_locks.Contains(Id))
                    return false;

                _locks.Add(Id);
                return true;
            }
        }

        public async Task ReleaseLock(string Id)
        {
            lock (_locks)
            {
                _locks.Remove(Id);
            }
        }

        public async Task EnqueueForPublishing(EventPublication item)
        {
            _publishQueue.Enqueue(item);
        }

        public async Task EnqueueForDeferredPublishing(EventPublication item)
        {
            _deferredPublishQueue.Enqueue(item);
        }        

        public async Task<EventPublication> DequeueForPublishing()
        {
            EventPublication item;
            if (_publishQueue.TryDequeue(out item))
            {
                return item;
            }
            return null;
        }


        private void FlushDeferredPublications(object state)
        {
            EventPublication pub;
            while (_deferredPublishQueue.TryDequeue(out pub))
                _publishQueue.Enqueue(pub);
        }

    }
}
