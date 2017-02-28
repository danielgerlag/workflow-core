using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowExecutor : IWorkflowExecutor
    {

        protected readonly IWorkflowHost _host;        
        protected readonly IWorkflowRegistry _registry;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;

        public WorkflowExecutor(IWorkflowHost host, IWorkflowRegistry registry, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _host = host;            
            _serviceProvider = serviceProvider;
            _registry = registry;
            _logger = loggerFactory.CreateLogger<WorkflowExecutor>();
        }

        public async Task Execute(WorkflowInstance workflow, IPersistenceProvider persistenceStore, WorkflowOptions options)
        {
            //TODO: split this method up
            List<ExecutionPointer> exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < DateTime.Now.ToUniversalTime())));
            var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
            if (def == null)
            {
                _logger.LogError("Workflow {0} version {1} is not registered", workflow.WorkflowDefinitionId, workflow.Version);
                return;
            }

            foreach (var pointer in exePointers)
            {
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step != null)
                {
                    try
                    {
                        if (step.InitForExecution(_host, persistenceStore, def, workflow, pointer) == ExecutionPipelineDirective.Defer)
                            continue;

                        if (!pointer.StartTime.HasValue)
                            pointer.StartTime = DateTime.Now;

                        _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                        IStepBody body = step.ConstructBody(_serviceProvider);

                        if (body == null)
                        {
                            _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                            pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                            pointer.Errors.Add(new ExecutionError()
                            {
                                Id = Guid.NewGuid().ToString(),
                                ErrorTime = DateTime.Now.ToUniversalTime(),
                                Message = String.Format("Unable to construct step body {0}", step.BodyType.ToString())
                            });
                            continue;
                        }

                        ProcessInputs(workflow, step, body);

                        IStepExecutionContext context = new StepExecutionContext()
                        {
                            Workflow = workflow,
                            Step = step,
                            PersistenceData = pointer.PersistenceData
                        };

                        if (step.BeforeExecute(_host, persistenceStore, context, pointer, body) == ExecutionPipelineDirective.Defer)
                            continue;

                        var result = body.Run(context);

                        ProcessOutputs(workflow, step, body);
                        ProcessExecutionResult(workflow, def, pointer, step, result);
                        step.AfterExecute(_host, persistenceStore, context, result, pointer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                        pointer.Errors.Add(new ExecutionError()
                        {
                            Id = Guid.NewGuid().ToString(),
                            ErrorTime = DateTime.Now.ToUniversalTime(),
                            Message = ex.Message
                        });

                        switch (step.ErrorBehavior ?? def.DefaultErrorBehavior)
                        {
                            case WorkflowErrorHandling.Retry:
                                pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? options.ErrorRetryInterval);
                                break;
                            case WorkflowErrorHandling.Suspend:
                                workflow.Status = WorkflowStatus.Suspended;
                                break;
                            case WorkflowErrorHandling.Terminate:
                                workflow.Status = WorkflowStatus.Terminated;
                                break;
                        }

                        _host.ReportStepError(workflow, step, ex);
                    }

                    await persistenceStore.PersistWorkflow(workflow);
                }
                else
                {
                    _logger.LogError("Unable to find step {0} in workflow definition", pointer.StepId);
                    pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                    pointer.Errors.Add(new ExecutionError()
                    {
                        ErrorTime = DateTime.Now.ToUniversalTime(),
                        Message = String.Format("Unable to find step {0} in workflow definition", pointer.StepId)
                    });
                }

            }
            DetermineNextExecutionTime(workflow);
            await persistenceStore.PersistWorkflow(workflow);
        }

        private void ProcessExecutionResult(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, ExecutionResult result)
        {
            if (result.Proceed)
            {
                pointer.Active = false;
                pointer.EndTime = DateTime.Now;
                int forkCounter = 1;
                bool noOutcomes = true;
                foreach (var outcome in step.Outcomes.Where(x => object.Equals(x.Value, result.OutcomeValue)))
                {
                    workflow.ExecutionPointers.Add(new ExecutionPointer()
                    {
                        Id = Guid.NewGuid().ToString(),
                        StepId = outcome.NextStep,
                        Active = true,
                        ConcurrentFork = (forkCounter * pointer.ConcurrentFork),
                        StepName = def.Steps.First(x => x.Id == outcome.NextStep).Name
                    });
                    noOutcomes = false;
                    forkCounter++;
                }
                pointer.PathTerminator = noOutcomes;
            }
            else
            {
                pointer.PersistenceData = result.PersistenceData;
                if (result.SleepFor.HasValue)
                    pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(result.SleepFor.Value);
            }
        }

        private void ProcessInputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body)
        {
            foreach (var input in step.Inputs)
            {
                var member = (input.Target.Body as MemberExpression);
                var resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data);
                step.BodyType.GetProperty(member.Member.Name).SetValue(body, resolvedValue);
            }
        }

        private void ProcessOutputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body)
        {
            foreach (var output in step.Outputs)
            {
                var member = (output.Target.Body as MemberExpression);
                var resolvedValue = output.Source.Compile().DynamicInvoke(body);
                var data = workflow.Data;
                data.GetType().GetProperty(member.Member.Name).SetValue(data, resolvedValue);
            }
        }

        private void DetermineNextExecutionTime(WorkflowInstance workflow)
        {
            workflow.NextExecution = null;

            foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active))
            {
                if (!pointer.SleepUntil.HasValue)
                {
                    workflow.NextExecution = 0;
                    return;
                }

                long pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
                workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
            }

            if (workflow.NextExecution == null)
            {
                int forks = 1;
                int terminals = 0;
                foreach (var pointer in workflow.ExecutionPointers)
                {
                    forks = Math.Max(pointer.ConcurrentFork, forks);
                    if (pointer.PathTerminator)
                        terminals++;
                }
                if (forks <= terminals)
                {
                    workflow.Status = WorkflowStatus.Complete;
                    workflow.CompleteTime = DateTime.Now.ToUniversalTime();
                }
            }
        }

    }
}
