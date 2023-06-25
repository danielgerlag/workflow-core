using System;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.PostgreSQL
{
    [Collection("Postgres collection")]
    public class PostgresPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly IPersistenceProvider _subject;
        protected override IPersistenceProvider Subject => _subject;

        public PostgresPersistenceProviderFixture(PostgresDockerSetup dockerSetup, ITestOutputHelper output)
        {
            output.WriteLine($"Connecting on {PostgresDockerSetup.ConnectionString}");
            _subject = new EntityFrameworkPersistenceProvider(new PostgresContextFactory(PostgresDockerSetup.ConnectionString,"wfc"), new ModelConverterService(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }),true, true);
            _subject.EnsureStoreExists();
        }
    }
}
