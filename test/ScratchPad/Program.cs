using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.ZeroMQ.Services;
using WorkflowCore.QueueProviders.ZeroMQ.Services;

namespace ScratchPad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddConsole(LogLevel.Debug);

            IQueueProvider Peer1 = new ZeroMQProvider(4001, "localhost:4002;localhost:4003".Split(';'), true, lf);
            IQueueProvider Peer2 = new ZeroMQProvider(4002, "localhost:4001;localhost:4003".Split(';'), true, lf);
            IQueueProvider Peer3 = new ZeroMQProvider(4003, "localhost:4001;localhost:4002".Split(';'), true, lf);

            Peer1.Start();            
            Peer2.Start();            
            Peer3.Start();
            System.Threading.Thread.Sleep(500);

            Peer1.QueueWork("Task 1", QueueType.Workflow).Wait();
            Peer1.QueueWork("Task 2", QueueType.Workflow).Wait();
            Peer1.QueueWork("Task 3", QueueType.Workflow).Wait();
            System.Threading.Thread.Sleep(100);

            var value1 = Peer1.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            var value2 = Peer2.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            var value3 = Peer3.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            
            Console.ReadLine();
        }
    }
}

