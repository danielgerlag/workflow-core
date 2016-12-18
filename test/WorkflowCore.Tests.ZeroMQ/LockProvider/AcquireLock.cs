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
    public class AcquireLock
    {
        Establish context = () =>
        {
            LoggerFactory lf = new LoggerFactory();
            lf.AddDebug();            
            Peer1 = new ZeroMQLockProvider(5001, "localhost:5002;localhost:5003".Split(';'), lf);
            Peer2 = new ZeroMQLockProvider(5002, "localhost:5001;localhost:5003".Split(';'), lf);
            Peer3 = new ZeroMQLockProvider(5003, "localhost:5001;localhost:5002".Split(';'), lf);

            Peer1.Start();
            Peer2.Start();
            Peer3.Start();
            System.Threading.Thread.Sleep(1000);
        };

        Because of = () => lock_result = Peer1.AcquireLock("lock1").Result;

        It should_return_true = () => lock_result.ShouldBeTrue();
        It should_be_locked_on_peer1 = () => Peer1.AcquireLock("lock1").Result.ShouldBeFalse();
        It should_be_locked_on_peer2 = () => Peer2.AcquireLock("lock1").Result.ShouldBeFalse();
        It should_be_locked_on_peer3 = () => Peer3.AcquireLock("lock1").Result.ShouldBeFalse();
        It should_have_another_id_unlocked_on_peer3 = () => Peer3.AcquireLock("lock2").Result.ShouldBeTrue();

        Cleanup after = () =>
        {
            Peer1.Stop();
            Peer2.Stop();
            Peer3.Stop();
        };

        static IDistributedLockProvider Peer1;
        static IDistributedLockProvider Peer2;
        static IDistributedLockProvider Peer3;

        static bool lock_result;



    }
}
