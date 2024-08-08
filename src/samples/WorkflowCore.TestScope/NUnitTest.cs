using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using WorkflowCore.TestScope.Workflow;

namespace WorkflowCore.TestScope
{
    [TestFixture]
    public class NUnitTest : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private IWorkflowHost _host;
        private IPersistenceProvider _persistenceProvider;
        private List<StepError> _unhandledStepErrors = new();

        [SetUp]
        protected void Setup()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow(options => options.UsePollInterval(TimeSpan.FromSeconds(3)));
            services.AddScoped<CountService>();
            services.AddTransient<HelloWorld>();
            services.AddTransient<GoodbyeWorld>();

            _serviceProvider = services.BuildServiceProvider();

            _persistenceProvider = _serviceProvider.GetService<IPersistenceProvider>();
            _host = _serviceProvider.GetService<IWorkflowHost>();
            _host.RegisterWorkflow<HelloWorldWorkflow, WorkflowData>();
            _host.RegisterWorkflow<GoodbyeWorldWorkflow, WorkflowData>();

            _host.OnStepError += Host_OnStepError;
            _host.Start();
        }

        [Test]
        public void NUnit_workflow_scope_test_sample()
        {
            using var scope1 = _serviceProvider.CreateScope();
            var countService1 = scope1.ServiceProvider.GetRequiredService<CountService>();
            using var scope2 = _serviceProvider.CreateScope();
            var countService2 = scope2.ServiceProvider.GetRequiredService<CountService>();

            var helloWorldWorkflowId = _host.StartWorkflowWithScope(scope1, "HelloWorld", new WorkflowData { ExecuteTimes = 6 }).Result;

            var goodbyeWorldWorkflowId = _host.StartWorkflowWithScope(scope2, "GoodbyeWorld", new WorkflowData { ExecuteTimes = 8 }).Result;

            WaitForWorkflowToComplete(helloWorldWorkflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(goodbyeWorldWorkflowId, TimeSpan.FromSeconds(30));

            GetStatus(helloWorldWorkflowId).Should().Be(WorkflowStatus.Complete);
            countService1.Count.Should().Be(6);

            GetStatus(goodbyeWorldWorkflowId).Should().Be(WorkflowStatus.Complete);
            countService2.Count.Should().Be(8);

            _unhandledStepErrors.Count.Should().Be(0);
        }

        private void Host_OnStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            _unhandledStepErrors.Add(new StepError
            {
                Exception = exception,
                Step = step,
                Workflow = workflow
            });
        }

        private void WaitForWorkflowToComplete(string workflowId, TimeSpan timeOut)
        {
            var status = GetStatus(workflowId);
            var counter = 0;
            while ((status == WorkflowStatus.Runnable) && (counter < (timeOut.TotalMilliseconds / 100)))
            {
                Thread.Sleep(100);
                counter++;
                status = GetStatus(workflowId);
            }
        }

        private WorkflowStatus GetStatus(string workflowId)
        {
            var instance = _persistenceProvider.GetWorkflowInstance(workflowId).Result;
            return instance.Status;
        }

        public void Dispose()
        {
            _host.Stop();
        }
    }
}

