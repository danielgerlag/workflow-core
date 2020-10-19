using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using FluentAssertions;
using NUnit.Framework;

namespace WorkflowCore.TestAssets.LockProvider
{
    public abstract class DistributedLockProviderTests
    {
        protected IDistributedLockProvider Subject;

        [SetUp]
        public void Setup()
        {
            Subject = CreateProvider();
            Subject.Start();
        }

        protected abstract IDistributedLockProvider CreateProvider();

        [Test]
        public async Task AcquiresLock()
        {
            const string lock1 = "lock1";
            const string lock2 = "lock2";
            await Subject.AcquireLock(lock2, new CancellationToken());

            var acquired = await Subject.AcquireLock(lock1, new CancellationToken());

            acquired.Should().Be(true);
        }

        [Test]
        public async Task DoesNotAcquireWhenLocked()
        {
            const string lock1 = "lock1";
            await Subject.AcquireLock(lock1, new CancellationToken());

            var acquired = await Subject.AcquireLock(lock1, new CancellationToken());

            acquired.Should().Be(false);
        }

        [Test]
        public async Task ReleasesLock()
        {
            const string lock1 = "lock1";
            await Subject.AcquireLock(lock1, new CancellationToken());

            await Subject.ReleaseLock(lock1);

            var available = await Subject.AcquireLock(lock1, new CancellationToken());
            available.Should().Be(true);
        }

        [TearDown]
        public virtual void TearDown()
        {
            Subject.Stop();
        }
    }
}
