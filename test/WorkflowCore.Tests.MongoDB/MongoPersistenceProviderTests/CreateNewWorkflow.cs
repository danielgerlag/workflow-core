using Machine.Specifications;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.TestAssets.Persistence;

namespace WorkflowCore.Tests.MongoDB.MongoPersistenceProviderTests
{
    [Subject(typeof(MongoPersistenceProvider))]
    public class CreateNewWorkflow
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static string workflowId;

        Establish context = () =>
        {
            var client = new MongoClient("mongodb://localhost:" + DockerSetup.Port);
            var db = client.GetDatabase("workflow-tests");

            Subject = new MongoPersistenceProvider(db);
            workflow = new WorkflowInstance()
            {
                Data = new { Value1 = 7 },
                Description = "My Description",
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Version = 1,
                WorkflowDefinitionId = "My Workflow"
            };
            workflow.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                Active = true,
                StepId = 0
            });
        };

        Because of = () => workflowId = Subject.CreateNewWorkflow(workflow).Result;

        Behaves_like<CreateNewWorkflowBehaviors> a_new_workflow;
        

    }
}
