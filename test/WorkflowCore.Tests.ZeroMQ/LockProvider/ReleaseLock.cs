using Machine.Specifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.ZeroMQ.Services;

namespace WorkflowCore.Tests.ZeroMQ.LockProvider
{
    [Subject(typeof(ZeroMQLockProvider))]
    public class ReleaseLock
    {
        Establish context = () =>
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddDebug();
            Peer1 = new ZeroMQLockProvider(5101, "localhost:5102;localhost:5103".Split(';'), lf);
            Peer2 = new ZeroMQLockProvider(5102, "localhost:5101;localhost:5103".Split(';'), lf);
            Peer3 = new ZeroMQLockProvider(5103, "localhost:5101;localhost:5102".Split(';'), lf);

            Peer1.Start();
            Peer2.Start();
            Peer3.Start();
            System.Threading.Thread.Sleep(1000);
            Peer1.AcquireLock("lock1").Wait();
        };

        Because of = () => Peer1.ReleaseLock("lock1").Wait();
                
        It should_be_lockable_on_peer1 = () => Peer1.AcquireLock("lock1").Result.ShouldBeTrue();        

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
