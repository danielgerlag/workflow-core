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
    /// Single node in-memory implementation of IQueueProvider
    /// </summary>
    public class SingleNodeQueueProvider : IQueueProvider
    {
                
        private ConcurrentQueue<string> _runQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<EventPublication> _publishQueue = new ConcurrentQueue<EventPublication>();
                

        public async Task QueueForProcessing(string Id)
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
                
        public async Task QueueForPublishing(EventPublication item)
        {
            _publishQueue.Enqueue(item);
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

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {            
        }

        
    }
}
