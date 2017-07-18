using Machine.Specifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.ZeroMQ.Services;

namespace WorkflowCore.Tests.ZeroMQ.LockProvider
{
    [Subject(typeof(ZeroMQLockProvider))]
    public class ReleaseLock_Exclusive
    {
        Establish context = () =>
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddDebug();
            Peer1 = new ZeroMQLockProvider(5201, "localhost:5202;localhost:5203".Split(';'), lf);
            Peer2 = new ZeroMQLockProvider(5202, "localhost:5201;localhost:5203".Split(';'), lf);
            Peer3 = new ZeroMQLockProvider(5203, "localhost:5201;localhost:5202".Split(';'), lf);

            Peer1.Start();
            Peer2.Start();
            Peer3.Start();
            System.Threading.Thread.Sleep(1000);
            Peer1.AcquireLock("lock1", new CancellationToken()).Wait();
        };

        Because of = () => Peer2.ReleaseLock("lock1").Wait();

        It should_not_be_lockable_on_peer2 = () => Peer2.AcquireLock("lock1", new CancellationToken()).Result.ShouldBeFalse();

        Cleanup after = () =>
        {
            Peer1.Stop();
            Peer2.Stop();
            Peer3.Stop();
        };

        static IDistributedLockProvider Peer1;
        static IDistributedLockProvider Peer2;
        static IDistributedLockProvider Peer3;
        
    }
}
