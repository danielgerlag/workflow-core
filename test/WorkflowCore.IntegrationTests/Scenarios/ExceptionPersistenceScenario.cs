using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ExceptionPersistenceWorkflow : IWorkflow
    {
        internal static int ExceptionThrownStep = 0;

        public string Id => "ExceptionPersistenceWorkflow";
        public int Version => 1;
        public void Build(IWorkflowBuilder<Object> builder)
        {
            builder
                .StartWith<ExceptionThrownStep>()
                    .CancelCondition(o => ExceptionThrownStep == 3)
                    .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(5))
                .Then(context =>
                {
                    return ExecutionResult.Next();
                });
        }
    }

    internal class ExceptionThrownStep : StepBody
    {
        public const string HelpLink = "help link";
        public const string ExceptionMessage = "ExceptionThrownStep is not implemented.";

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            ExceptionPersistenceWorkflow.ExceptionThrownStep++;

            throw new NotImplementedException(ExceptionMessage)
            {
                HelpLink = HelpLink
            };

            return ExecutionResult.Next();
        }
    }

    public class ExceptionPersistenceScenario : WorkflowTest<ExceptionPersistenceWorkflow, Object>
    {   
        public ExceptionPersistenceScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            ExceptionPersistenceWorkflow.ExceptionThrownStep = 0;

            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(60));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(3);
            ExceptionPersistenceWorkflow.ExceptionThrownStep.Should().Be(3);

            var persistedExecutionErrors = PersistenceProvider.GetExecutionErrors(workflowId).Result.ToList();
            var workflowInstance = this.PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            //ExecutionErrorCount is persisted on workflow instance, even if the execution error details are not.
            workflowInstance.ExecutionErrorCount.Should().Be(3);
    
            if (PersistenceProvider.SupportsPersistingErrors)
            {
                //Providers that support Persistence of Execution Errors
                persistedExecutionErrors.Count().Should().Be(3);
                persistedExecutionErrors.ForEach(error =>
                {
                    error.Should().NotBe(null);
                    error.Message.Should().Be(ExceptionThrownStep.ExceptionMessage);
                    error.HelpLink.Should().Be(ExceptionThrownStep.HelpLink);
                    error.TargetSiteName.Should().NotBeNullOrEmpty();
                    error.TargetSiteModule.Should().NotBeNullOrEmpty();
                    error.Type.Should().Be(typeof(NotImplementedException).FullName);
                    error.Source.Should().NotBeNullOrEmpty();
                    error.WorkflowId.Should().Be(workflowId);
                    error.StackTrace.Should().NotBeNullOrEmpty();
                    error.ErrorTime.Should().BeBefore(DateTime.UtcNow).And.BeAfter(DateTime.MinValue);
                });
            }
            else
            {
                //Providers that DO NOT support Persistence of Execution Errors
                persistedExecutionErrors.Count.Should().Be(0);
            }
        }
    }
}
