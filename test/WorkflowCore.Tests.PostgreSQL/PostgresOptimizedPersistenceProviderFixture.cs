using System;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.PostgreSQL
{
    [Collection(PostgresCollection.Name)]
    public class PostgresOptimizedPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly IPersistenceProvider _subject;
        protected override IPersistenceProvider Subject => _subject;

        public PostgresOptimizedPersistenceProviderFixture(PostgresDockerSetup dockerSetup, ITestOutputHelper output)
        {
            output.WriteLine($"Connecting on {PostgresDockerSetup.ConnectionString}");
            _subject = new LargeDataOptimizedEntityFrameworkPersistenceProvider(new PostgresContextFactory(PostgresDockerSetup.ConnectionString,"wfc"), true, true);
            _subject.EnsureStoreExists();
        }
    }
}
