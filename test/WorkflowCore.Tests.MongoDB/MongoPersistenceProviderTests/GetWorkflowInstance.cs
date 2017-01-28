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
    public class GetWorkflowInstance
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static WorkflowInstance retrievedWorkflow;
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
                WorkflowDefinitionId = "My Workflow",
                CreateTime = new DateTime(2000, 1, 1).ToUniversalTime()
            };

            var ep = new ExecutionPointer()
            {
                Active = true,
                StepId = 0
            };

            ep.ExtensionAttributes["Attr1"] = "test";
            ep.ExtensionAttributes["Attr2"] = 5;
            workflow.ExecutionPointers.Add(ep);

            workflowId = Subject.CreateNewWorkflow(workflow).Result;
        };

        Because of = () => retrievedWorkflow = Subject.GetWorkflowInstance(workflowId).Result;

        Behaves_like<GetWorkflowInstanceBehaviors> get_workflow_instance;

        Cleanup after = () =>
        {
            Subject = null;
            workflow = null;
            retrievedWorkflow = null;
            workflowId = null;
        };
                

    }
}
