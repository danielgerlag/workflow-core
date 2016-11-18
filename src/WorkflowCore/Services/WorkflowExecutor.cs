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
                        if ((step is ISubscriptionStep) && (!pointer.EventPublished))
                        {
                            pointer.EventKey = (step as ISubscriptionStep).EventKey;
                            pointer.EventName = (step as ISubscriptionStep).EventName;
                            pointer.Active = false;
                            await persistenceStore.PersistWorkflow(workflow);
                            await _host.SubscribeEvent(workflow.Id, pointer.StepId, pointer.EventName, pointer.EventKey);
                            continue;
                        }

                        if (!pointer.StartTime.HasValue)
                            pointer.StartTime = DateTime.Now;

                        _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                        IStepBody body;

                        if (step is WorkflowStepInline)
                        {
                            body = new InlineStepBody((step as WorkflowStepInline).Body);
                        }
                        else
                        {
                            body = (_serviceProvider.GetService(step.BodyType) as IStepBody);
                            if (body == null)
                            {
                                var stepCtor = step.BodyType.GetConstructor(new Type[] { });
                                if (stepCtor != null)
                                    body = (stepCtor.Invoke(null) as IStepBody);
                            }

                            if (body == null)
                            {
                                _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                                pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                                pointer.Errors.Add(new ExecutionError()
                                {
                                    ErrorTime = DateTime.Now.ToUniversalTime(),
                                    Message = String.Format("Unable to construct step body {0}", step.BodyType.ToString())
                                });
                                continue;
                            }
                        }                        
                        
                        foreach (var input in step.Inputs)
                        {
                            var member = (input.Target.Body as MemberExpression);
                            var resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data);
                            step.BodyType.GetProperty(member.Member.Name).SetValue(body, resolvedValue);                            
                        }

                        if ((body is ISubscriptionBody) && (pointer.EventPublished))
                        {
                            (body as ISubscriptionBody).EventData = pointer.EventData;
                        }

                        IStepExecutionContext context = new StepExecutionContext()
                        {
                            Workflow = workflow,
                            Step = step,
                            PersistenceData = pointer.PersistenceData
                        };

                        var result = body.Run(context);
                                                
                        foreach (var output in step.Outputs)
                        {
                            var member = (output.Target.Body as MemberExpression);
                            var resolvedValue = output.Source.Compile().DynamicInvoke(body);
                            var data = workflow.Data;
                            data.GetType().GetProperty(member.Member.Name).SetValue(data, resolvedValue);
                        }

                        if (result.Proceed)
                        {
                            pointer.Active = false;
                            pointer.EndTime = DateTime.Now;
                            
                            foreach (var outcome in step.Outcomes.Where(x => object.Equals(x.Value, result.OutcomeValue)))
                            {
                                workflow.ExecutionPointers.Add(new ExecutionPointer()
                                {
                                    StepId = outcome.NextStep,
                                    Active = true
                                });
                            }
                        }
                        else
                        {
                            pointer.PersistenceData = result.PersistenceData;
                            if (result.SleepFor.HasValue)
                                pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(result.SleepFor.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                        pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                        pointer.Errors.Add(new ExecutionError()
                        {
                            ErrorTime = DateTime.Now.ToUniversalTime(),
                            Message = ex.Message
                        });
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
        }

    }
}
