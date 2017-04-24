using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using WorkflowCore.TestAssets.LockProvider;

namespace WorkflowCore.UnitTests.SingleNodeLockProviderTests
{
    [TestFixture]
    public class SingleNodeLockProviderTests : DistributedLockProviderTests
    {
        protected override IDistributedLockProvider CreateProvider()
        {
            return new SingleNodeLockProvider();
        }
    }
}
