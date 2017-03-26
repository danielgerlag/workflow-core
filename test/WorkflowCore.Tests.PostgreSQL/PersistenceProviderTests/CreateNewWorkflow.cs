using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.TestAssets.Persistence;

namespace WorkflowCore.Tests.PostgreSQL.PersistenceProviderTests
{
    [Subject(typeof(PostgresPersistenceProvider))]
    public class PostgreSQL_CreateNewWorkflow : CreateNewWorkflow
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

        Behaves_like<CreateNewWorkflowBehaviors> a_new_workflow;
    }    
}
