using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using Autofac;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class DiWorkflow : IWorkflow<DiData>
    {
        public string Id => "DiWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<DiData> builder)
        {
            builder
                .StartWith<DiStep1>()
                    .Output(_ => _.instance1, _ => _.dependency1.Instance)
                    .Output(_ => _.instance2, _ => _.dependency2.dependency1.Instance)
                .Then(context =>
                {
                    return ExecutionResult.Next();
                });
        }
    }

    public class DiData
    {
        public int instance1 { get; set; } = -1;
        public int instance2 { get; set; } = -1;
    }

    public class Dependency1
    {
        private static int InstanceCounter = 0;

        public int Instance { get; set; } = ++InstanceCounter;
    }

    public class Dependency2
    {
        public Dependency1 dependency1 { get; private set; }

        public Dependency2(Dependency1 dependency1)
        {
            this.dependency1 = dependency1;
        }
    }

    public class DiStep1 : StepBody
    {
        public Dependency1 dependency1 { get; private set; }
        public Dependency2 dependency2 { get; private set; }

        public DiStep1(Dependency1 dependency1, Dependency2 dependency2)
        {
            this.dependency1 = dependency1;
            this.dependency2 = dependency2;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Next();
        }
    }

    /// <summary>
    /// The DI scenarios are design to test whether the scoped / transient dependecies are honoured with
    /// various IoC container implementations. The basic premise is that a step has a dependency on
    /// two services, one of which has a dependency on the other.
    /// 
    /// We then use the instance numbers of the services to determine whether the container has created a
    /// transient instance or a scoped instance
    /// 
    /// if step.dependency2.dependency1.instance == step.dependency1.instance then
    /// we can be assured that dependency1 was created in the same scope as dependency 2
    /// 
    /// otherwise if the instances are different, they were created as transient
    /// 
    /// </summary>
    public abstract class DiScenario : WorkflowTest<DiWorkflow, DiData>
    {
        protected void ConfigureHost(IServiceProvider serviceProvider)
        {
            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.RegisterWorkflow<DiWorkflow, DiData>();
            Host.OnStepError += Host_OnStepError;
            Host.Start();
        }
    }

    /// <summary>
    /// Because of the static InMemory Persistence provider, this test must run in issolation
    /// to prevent other hosts from picking up steps intended for this host and incorrectly 
    /// cross-referencing the scoped / transient IoC container for step constrcution
    /// </summary>
    [CollectionDefinition("DiMsTransientScenario", DisableParallelization = true)]
    [Collection("DiMsTransientScenario")]
    public class DiMsTransientScenario : DiScenario
    {
        public DiMsTransientScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddTransient<Dependency1>();
            services.AddTransient<Dependency2>();
            services.AddTransient<DiStep1>();
            services.AddLogging();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            ConfigureHost(serviceProvider);
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new DiData());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(5));
            var data = GetData(workflowId);

            // DI provider should have created two transient instances, with different instance ids
            data.instance1.Should().NotBe(-1);
            data.instance2.Should().NotBe(-1);
            data.instance1.Should().NotBe(data.instance2);
        }
    }

    /// <summary>
    /// Because of the static InMemory Persistence provider, this test must run in issolation
    /// to prevent other hosts from picking up steps intended for this host and incorrectly 
    /// cross-referencing the scoped / transient IoC container for step constrcution
    /// </summary>
    [CollectionDefinition("DiMsScopedScenario", DisableParallelization = true)]
    [Collection("DiMsScopedScenario")]
    public class DiMsScopedScenario : DiScenario
    {
        public DiMsScopedScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddScoped<Dependency1>();
            services.AddScoped<Dependency2>();
            services.AddTransient<DiStep1>();
            services.AddLogging();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            ConfigureHost(serviceProvider);
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new DiData());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(5));
            var data = GetData(workflowId);

            // scope provider should have created one scoped instance, with the same instance ids
            data.instance1.Should().NotBe(-1);
            data.instance2.Should().NotBe(-1);
            data.instance1.Should().Be(data.instance2);
        }
    }

    /// <summary>
    /// Because of the static InMemory Persistence provider, this test must run in issolation
    /// to prevent other hosts from picking up steps intended for this host and incorrectly 
    /// cross-referencing the scoped / transient IoC container for step constrcution
    /// </summary>
    [CollectionDefinition("DiAutoFacTransientScenario", DisableParallelization = true)]
    [Collection("DiAutoFacTransientScenario")]
    public class DiAutoFacTransientScenario : DiScenario
    {
        public DiAutoFacTransientScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            ConfigureServices(services);

            //setup dependency injection
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterType<Dependency1>().InstancePerDependency();
            builder.RegisterType<Dependency2>().InstancePerDependency();
            builder.RegisterType<DiStep1>().InstancePerDependency();
            var container = builder.Build();

            var serviceProvider = new AutofacServiceProvider(container);
            ConfigureHost(serviceProvider);
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new DiData());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(5));
            var data = GetData(workflowId);

            // scope provider should have created one scoped instance, with the same instance ids
            data.instance1.Should().NotBe(-1);
            data.instance2.Should().NotBe(-1);
            data.instance1.Should().NotBe(data.instance2);
        }
    }

    /// <summary>
    /// Because of the static InMemory Persistence provider, this test must run in issolation
    /// to prevent other hosts from picking up steps intended for this host and incorrectly 
    /// cross-referencing the scoped / transient IoC container for step constrcution
    /// </summary>
    [CollectionDefinition("DiAutoFacScopedScenario", DisableParallelization = true)]
    [Collection("DiAutoFacScopedScenario")]
    public class DiAutoFacScopedScenario : DiScenario
    {
        public DiAutoFacScopedScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            ConfigureServices(services);

            //setup dependency injection
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterType<Dependency1>().InstancePerLifetimeScope();
            builder.RegisterType<Dependency2>().InstancePerLifetimeScope();
            builder.RegisterType<DiStep1>().InstancePerLifetimeScope();
            var container = builder.Build();

            var serviceProvider = new AutofacServiceProvider(container);
            ConfigureHost(serviceProvider);
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(5));
            var data = GetData(workflowId);

            // scope provider should have created one scoped instance, with the same instance ids
            data.instance1.Should().NotBe(-1);
            data.instance2.Should().NotBe(-1);
            data.instance1.Should().Be(data.instance2);
        }
    }
}
