using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Oracle;
using WorkflowCore.UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.Oracle
{
    [Collection("Oracle collection")]
    public class OraclePersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly EntityFrameworkPersistenceProvider _subject;
        protected override IPersistenceProvider Subject => _subject;

        public OraclePersistenceProviderFixture(OracleDockerSetup dockerSetup, ITestOutputHelper output)
        {
            output.WriteLine($"Connecting on {OracleDockerSetup.ConnectionString}");
            _subject = new EntityFrameworkPersistenceProvider(new OracleContextFactory(OracleDockerSetup.ConnectionString), true, true);
            _subject.EnsureStoreExists();
        }
    }
}
