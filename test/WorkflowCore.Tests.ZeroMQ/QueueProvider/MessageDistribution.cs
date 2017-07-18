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
    public class MessageDistribution
    {
        Establish context = () =>
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddDebug();

            Peer1 = new ZeroMQProvider(4001, "localhost:4002;localhost:4003".Split(';'), true, lf);
            Peer2 = new ZeroMQProvider(4002, "localhost:4001;localhost:4003".Split(';'), true, lf);
            Peer3 = new ZeroMQProvider(4003, "localhost:4001;localhost:4002".Split(';'), true, lf);

            Peer1.Start();
            Peer2.Start();
            Peer3.Start();
            System.Threading.Thread.Sleep(100);
        };

        Because of = () =>
        {
            Peer1.QueueWork("Task 1", QueueType.Workflow).Wait();
            System.Threading.Thread.Sleep(100);
        };

        It should_be_dequeued_once_on_any_peer = () =>
        {
            var result1 = Peer1.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            var result2 = Peer2.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            var result3 = Peer3.DequeueWork(QueueType.Workflow, new CancellationToken()).Result;
            var oneResult = (result1 == "Task 1") ^ (result2 == "Task 1") ^ (result3 == "Task 1");
            oneResult.ShouldBeTrue();
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
