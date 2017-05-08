using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    [Collection("Mongo collection")]
    public class MongoPersistenceProviderFixture : BasePersistenceFixture
    {
        DockerSetup _dockerSetup;

        public MongoPersistenceProviderFixture(DockerSetup dockerSetup)
        {
            _dockerSetup = dockerSetup;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                var client = new MongoClient(_dockerSetup.ConnectionString);
                var db = client.GetDatabase("workflow-tests");
                return new MongoPersistenceProvider(db);
            }
        }
    }
}
