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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /// <summary>
    /// Single node in-memory implementation of IQueueProvider
    /// </summary>
    public class SingleNodeQueueProvider : IQueueProvider
    {
                
        private ConcurrentQueue<string> _runQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();        

        public async Task QueueWork(string id, QueueType queue)
        {
            SelectQueue(queue).Enqueue(id);
        }

        public async Task<string> DequeueWork(QueueType queue)
        {
            if (SelectQueue(queue).TryDequeue(out string id))
                return id;

            return null;
        }
        
        public async Task Start()
        {
        }

        public async Task Stop()
        {
        }

        public void Dispose()
        {            
        }

        private ConcurrentQueue<string> SelectQueue(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return _runQueue;                    
                case QueueType.Event:
                    return _eventQueue;
            }
            return null;
        }
        
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
