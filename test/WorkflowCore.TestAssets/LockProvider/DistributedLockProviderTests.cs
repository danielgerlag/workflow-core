using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using NUnit;
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
        public async void AcquiresLock()
        {
            const string lock1 = "lock1";
            const string lock2 = "lock2";
            await Subject.AcquireLock(lock2);

            var acquired = await Subject.AcquireLock(lock1);

            acquired.Should().Be(true);
        }

        [Test]
        public async void DoesNotAcquireWhenLocked()
        {
            const string lock1 = "lock1";
            await Subject.AcquireLock(lock1);

            var acquired = await Subject.AcquireLock(lock1);

            acquired.Should().Be(false);
        }

        [Test]
        public async void ReleasesLock()
        {
            const string lock1 = "lock1";
            await Subject.AcquireLock(lock1);

            await Subject.ReleaseLock(lock1);

            var available = await Subject.AcquireLock(lock1);
            available.Should().Be(true);
        }

        [TearDown]
        public virtual void TearDown()
        {
            Subject.Stop();
        }
    }
}
