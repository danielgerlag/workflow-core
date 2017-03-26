using Machine.Fakes;
using Machine.Fakes.Adapters.Moq;
using Machine.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Users.Models;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    [Behaviors]
    public class UserStepsBehavior
    {
        protected static int ApproveStepTicker = 0;
        protected static int DisapproveStepTicker = 0;
        protected static IEnumerable<OpenUserAction> openItems1;
        protected static IEnumerable<OpenUserAction> openItems2;

        It should_be_marked_as_approved = () => ApproveStepTicker.ShouldEqual(1);
        It should_not_be_marked_as_disapproved = () => DisapproveStepTicker.ShouldEqual(0);
        It should_have_return_open_item = () => openItems1.Count().ShouldEqual(1);
        It should_have_return_2_options = () => openItems1.First().Options.Count().ShouldEqual(2);
        It should_have_yes_option = () => openItems1.First().Options.ShouldContain(x => Convert.ToString(x.Value) == "yes");
        It should_have_no_option = () => openItems1.First().Options.ShouldContain(x => Convert.ToString(x.Value) == "no");
    }

    [Subject(typeof(WorkflowHost))]
    public class UserStepsTest : BaseScenario<UserStepsTest.HumanWorkflow, object>
    {
        protected static string WorkflowId;
        protected static WorkflowInstance Instance;

        public class HumanWorkflow : IWorkflow
        {
            public string Id => "HumanWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                .StartWith(context => ExecutionResult.Next())
                .UserStep("Do you approve", data => "user1", x => x.Name("Approval Step"))
                    .When("yes", "I approve")
                        .Then(context =>
                        {
                            ApproveStepTicker++;
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step")
                    .When("no", "I do not approve")
                        .Then(context =>
                        {
                            DisapproveStepTicker++;
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step");
            }
        }

        protected static int ApproveStepTicker = 0;
        protected static int DisapproveStepTicker = 0;
        protected static IEnumerable<OpenUserAction> openItems1;
        protected static IEnumerable<OpenUserAction> openItems2;
                
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow();
        }        

        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("HumanWorkflow").Result;

            int counter = 0;
            
            while ((Host.GetOpenUserActions(WorkflowId).Count() == 0) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;                
            }

            openItems1 = Host.GetOpenUserActions(WorkflowId);

            Host.PublishUserAction(openItems1.First().Key, "user1", "yes").Wait();

            Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;
            counter = 0;
            while ((Instance.Status == WorkflowStatus.Runnable) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;
                Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;                
            }

            openItems2 = Host.GetOpenUserActions(WorkflowId);
        };

        Behaves_like<UserStepsBehavior> user_steps_workflow;
        
    }
}
