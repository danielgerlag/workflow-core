using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.ZeroMQ.Services;

namespace ScratchPad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoggerFactory lf = new LoggerFactory();            
            lf.AddConsole(LogLevel.Debug);

            IDistributedLockProvider Peer1 = new ZeroMQLockProvider(5001, "localhost:5002".Split(';'), lf);
            IDistributedLockProvider Peer2 = new ZeroMQLockProvider(5002, "localhost:5001".Split(';'), lf);
            
            Peer1.Start();
            Peer2.Start();           

            System.Threading.Thread.Sleep(2000);

            var lock_result0 = Peer1.AcquireLock("lock1").Result;
            var lock_result1 = Peer1.AcquireLock("lock1").Result;
            var lock_result2 = Peer2.AcquireLock("lock1").Result;
            
            var lock_result4 = Peer2.AcquireLock("lock2").Result;

            Peer1.ReleaseLock("lock1");            
            var lock_result5 = Peer2.AcquireLock("lock1").Result;



            Console.ReadLine();
        }
    }
}
