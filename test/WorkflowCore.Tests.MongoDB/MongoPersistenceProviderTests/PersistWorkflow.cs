using Machine.Specifications;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.Services;
using WorkflowCore.TestAssets;
using WorkflowCore.TestAssets.Persistence;

namespace WorkflowCore.Tests.MongoDB.MongoPersistenceProviderTests
{    
    [Subject(typeof(MongoPersistenceProvider))]    
    public class PersistWorkflow
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance newWorkflow;
        protected static string workflowId;

        Establish context = () =>
        {
            var client = new MongoClient("mongodb://localhost:" + DockerSetup.Port);
            var db = client.GetDatabase("workflow-tests");
            Subject = new MongoPersistenceProvider(db);

            var oldWorkflow = new WorkflowInstance()
            {
                Data = new { Value1 = 7 },
                Description = "My Description",
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Version = 1,
                WorkflowDefinitionId = "My Workflow",
                CreateTime = new DateTime(2000, 1, 1).ToUniversalTime()
            };
            oldWorkflow.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                Active = true,
                StepId = 0
            });

            workflowId = Subject.CreateNewWorkflow(oldWorkflow).Result;

            newWorkflow = Utils.DeepCopy(oldWorkflow);
            newWorkflow.NextExecution = 7;
            newWorkflow.ExecutionPointers.Add(new ExecutionPointer() { Id = Guid.NewGuid().ToString(), Active = true, StepId = 1 });
        };

        Because of = () => Subject.PersistWorkflow(newWorkflow).Wait();

        Behaves_like<PersistWorkflowBehaviors> persist_workflow;

        Cleanup after = () =>
        {
            Subject = null;
            newWorkflow = null;            
            workflowId = null;
        };
        

    }
}
