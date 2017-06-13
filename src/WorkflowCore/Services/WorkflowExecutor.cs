﻿using Microsoft.Extensions.Logging;
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

        public WorkflowExecutorResult Execute(WorkflowInstance workflow, WorkflowOptions options)
        {
            WorkflowExecutorResult wfResult = new WorkflowExecutorResult();

            List<ExecutionPointer> exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < DateTime.Now.ToUniversalTime())));
            var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
            if (def == null)
            {
                _logger.LogError("Workflow {0} version {1} is not registered", workflow.WorkflowDefinitionId, workflow.Version);
                return wfResult;
            }

            foreach (var pointer in exePointers)
            {
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step != null)
                {
                    try
                    {
                        switch (step.InitForExecution(wfResult, def, workflow, pointer))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = DateTime.Now.ToUniversalTime();
                                continue;
                        }

                        if (!pointer.StartTime.HasValue)
                            pointer.StartTime = DateTime.Now;

                        _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                        IStepBody body = step.ConstructBody(_serviceProvider);

                        if (body == null)
                        {
                            _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                            pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                            wfResult.Errors.Add(new ExecutionError()
                            {
                                WorkflowId = workflow.Id,
                                ExecutionPointerId = pointer.Id,
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
                            PersistenceData = pointer.PersistenceData,
                            ExecutionPointer = pointer,
                            Item = pointer.ContextItem
                        };

                        switch (step.BeforeExecute(wfResult, context, pointer, body))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = DateTime.Now.ToUniversalTime();
                                continue;
                        }

                        var result = body.Run(context);

                        ProcessOutputs(workflow, step, body);
                        ProcessExecutionResult(workflow, def, pointer, step, result);
                        step.AfterExecute(wfResult, context, result, pointer);
                    }
                    catch (Exception ex)
                    {
                        pointer.RetryCount++;
                        _logger.LogError("Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                        wfResult.Errors.Add(new ExecutionError()
                        {
                            WorkflowId = workflow.Id,
                            ExecutionPointerId = pointer.Id,
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
                }
                else
                {
                    _logger.LogError("Unable to find step {0} in workflow definition", pointer.StepId);
                    pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(options.ErrorRetryInterval);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = DateTime.Now.ToUniversalTime(),
                        Message = String.Format("Unable to find step {0} in workflow definition", pointer.StepId)
                    });
                }

            }
            DetermineNextExecutionTime(workflow);

            return wfResult;
        }

        private void ProcessExecutionResult(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, ExecutionResult result)
        {
            //TODO: refactor this into it's own class
            pointer.PersistenceData = result.PersistenceData;
            pointer.Outcome = result.OutcomeValue;
            if (result.SleepFor.HasValue)
                pointer.SleepUntil = DateTime.Now.ToUniversalTime().Add(result.SleepFor.Value);
            
            if (result.Proceed)
            {
                pointer.Active = false;
                pointer.EndTime = DateTime.Now.ToUniversalTime();                

                foreach (var outcomeTarget in step.Outcomes.Where(x => object.Equals(x.GetValue(workflow.Data), result.OutcomeValue) || x.GetValue(workflow.Data) == null))
                {
                    workflow.ExecutionPointers.Add(new ExecutionPointer()
                    {
                        Id = Guid.NewGuid().ToString(),
                        PredecessorId = pointer.Id,
                        StepId = outcomeTarget.NextStep,
                        Active = true,
                        ContextItem = pointer.ContextItem,
                        StepName = def.Steps.First(x => x.Id == outcomeTarget.NextStep).Name
                    });
                }
            }
            else
            {
                foreach (var branch in result.BranchValues)
                {
                    foreach (var childDefId in step.Children)
                    {
                        var childPointerId = Guid.NewGuid().ToString();
                        workflow.ExecutionPointers.Add(new ExecutionPointer()
                        {
                            Id = childPointerId,
                            PredecessorId = pointer.Id,
                            StepId = childDefId,
                            Active = true,
                            ContextItem = branch,
                            StepName = def.Steps.First(x => x.Id == childDefId).Name
                        });
                        pointer.Children.Add(childPointerId);
                    }
                }
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

            if (workflow.Status == WorkflowStatus.Complete)
                return;

            foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count == 0))
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
                foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count > 0))
                {
                    if (workflow.ExecutionPointers.Where(x => pointer.Children.Contains(x.Id)).All(x => IsBranchComplete(workflow.ExecutionPointers, x.Id)))
                    {
                        workflow.NextExecution = 0;
                        return;
                    }                    
                }
            }

            if ((workflow.NextExecution == null) && (workflow.ExecutionPointers.All(x => x.EndTime != null)))
            {
                workflow.Status = WorkflowStatus.Complete;
                workflow.CompleteTime = DateTime.Now.ToUniversalTime();
            }            
        }

        private bool IsBranchComplete(IEnumerable<ExecutionPointer> pointers, string rootId)
        {
            //TODO: move to own class
            var root = pointers.First(x => x.Id == rootId);

            if (root.EndTime == null)
                return false;

            var list = pointers.Where(x => x.PredecessorId == rootId).ToList();

            bool result = true;

            foreach (var item in list)
                result = result && IsBranchComplete(pointers, item.Id);

            return result;
        }

    }
}
