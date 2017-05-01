using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL
{
    [Collection("Postgres collection")]
    public class PostgresPersistenceProviderFixture : BasePersistenceFixture
    {
        DockerSetup _dockerSetup;

        public PostgresPersistenceProviderFixture(DockerSetup dockerSetup)
        {
            _dockerSetup = dockerSetup;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {                
                var db = new PostgresPersistenceProvider(_dockerSetup.ConnectionString, true, true);
                db.EnsureStoreExists();
                return db;
            }
        }
    }
}
