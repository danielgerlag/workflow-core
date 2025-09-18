﻿using MongoDB.Driver;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    [Collection("Mongo collection")]
    public class MongoPersistenceProviderFixture : BasePersistenceFixture
    {
        MongoDockerSetup _dockerSetup;

        public MongoPersistenceProviderFixture(MongoDockerSetup dockerSetup)
        {
            _dockerSetup = dockerSetup;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                var client = new MongoClient(MongoDockerSetup.ConnectionString);
                var db = client.GetDatabase(nameof(MongoPersistenceProviderFixture));
                var provider = new MongoPersistenceProvider(db);
                provider.EnsureStoreExists();
                return provider;
            }
        }
    }
}
