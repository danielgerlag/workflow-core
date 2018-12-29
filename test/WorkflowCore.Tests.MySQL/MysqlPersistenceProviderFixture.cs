using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.MySQL;
using WorkflowCore.UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.MySQL
{
    [Collection("Mysql collection")]
    public class MysqlPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly IPersistenceProvider _subject;
        protected override IPersistenceProvider Subject => _subject;

        public MysqlPersistenceProviderFixture(MysqlDockerSetup dockerSetup, ITestOutputHelper output)
        {
            output.WriteLine($"Connecting on {MysqlDockerSetup.ConnectionString}");
            _subject = new EntityFrameworkPersistenceProvider(new MysqlContextFactory(MysqlDockerSetup.ConnectionString), true, true);
            _subject.EnsureStoreExists();
        }
    }
}
