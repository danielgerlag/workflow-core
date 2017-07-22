using Machine.Specifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.ZeroMQ.Services;

namespace WorkflowCore.Tests.ZeroMQ.QueueProvider
{    
    [Subject(typeof(ZeroMQProvider))]
    public class MultiMessageDistribution
    {
        Establish context = () =>
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddDebug();

            Peer1 = new ZeroMQProvider(4101, "localhost:4102;localhost:4103".Split(';'), true, lf);
            Peer2 = new ZeroMQProvider(4102, "localhost:4101;localhost:4103".Split(';'), true, lf);
            Peer3 = new ZeroMQProvider(4103, "localhost:4101;localhost:4102".Split(';'), true, lf);

            Peer1.Start();
            Peer2.Start();
            Peer3.Start();
            System.Threading.Thread.Sleep(100);
        };

        Because of = () =>
        {
            Peer1.QueueWork("Task 1", QueueType.Workflow).Wait();
            Peer1.QueueWork("Task 2", QueueType.Workflow).Wait();
            Peer1.QueueWork("Task 3", QueueType.Workflow).Wait();
            System.Threading.Thread.Sleep(100);
        };

        It should_be_dequeued_once_on_any_peer = () =>
        {            
            string[] results = new string[] 
            {
                Peer1.DequeueWork(QueueType.Workflow, new CancellationToken()).Result,
                Peer2.DequeueWork(QueueType.Workflow, new CancellationToken()).Result,
                Peer3.DequeueWork(QueueType.Workflow, new CancellationToken()).Result
            };

            results.ShouldContain("Task 1");
            results.ShouldContain("Task 2");
            results.ShouldContain("Task 3");
        };
        
        Cleanup after = () =>
        {
            Peer1.Stop();
            Peer2.Stop();
            Peer3.Stop();
        };

        static IQueueProvider Peer1;
        static IQueueProvider Peer2;
        static IQueueProvider Peer3;
        

    }
}

