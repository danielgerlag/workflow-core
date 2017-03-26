using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.Services;
using WorkflowCore.TestAssets;
using WorkflowCore.TestAssets.Persistence;

namespace WorkflowCore.Tests.PostgreSQL.PersistenceProviderTests
{
    [Subject(typeof(PostgresPersistenceProvider))]
    public class PostgreSQL_PersistWorkflow : PersistWorkflow
    {
        protected override IPersistenceProvider Provider
        {
            get
            {
                var db = new PostgresPersistenceProvider("Server=127.0.0.1;Port=" + DockerSetup.Port + ";Database=workflow;User Id=postgres;", true, true);
                db.EnsureStoreExists();
                return db;
            }
        }

        Behaves_like<PersistWorkflowBehaviors> persist_workflow;
    }
}
