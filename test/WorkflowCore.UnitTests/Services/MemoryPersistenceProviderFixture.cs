using System;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.UnitTests.Services
{
    public class MemoryPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly IPersistenceProvider _subject = new MemoryPersistenceProvider();

        protected override IPersistenceProvider Subject => _subject;
    }
}
