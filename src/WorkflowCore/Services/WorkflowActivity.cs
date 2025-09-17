using System.Diagnostics;
using OpenTelemetry.Trace;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    internal static class WorkflowActivity
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("WorkflowCore");

        internal static Activity StartHost()
        {
            var activityName = "workflow start host";
            return ActivitySource.StartRootActivity(activityName, ActivityKind.Internal);
        }

        internal static Activity StartConsume(QueueType queueType)
        {
            var activityName = $"workflow consume {GetQueueType(queueType)}";
            var activity = ActivitySource.StartRootActivity(activityName, ActivityKind.Consumer);

            activity?.SetTag("workflow.queue", queueType);

            return activity;
        }

        
        internal static Activity StartPoll(string type)
        {
            var activityName = $"workflow poll {type}";
            var activity = ActivitySource.StartRootActivity(activityName, ActivityKind.Client);

            activity?.SetTag("workflow.poll", type);

            return activity;
        }

        internal static void Enrich(WorkflowInstance workflow, string action)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.DisplayName = $"workflow {action} {workflow.WorkflowDefinitionId}";
                activity.SetTag("workflow.id", workflow.Id);
                activity.SetTag("workflow.definition", workflow.WorkflowDefinitionId);
                activity.SetTag("workflow.status", workflow.Status);
            }
        }


        internal static void Enrich(WorkflowStep workflowStep)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var stepName = string.IsNullOrEmpty(workflowStep.Name)
                    ? "inline"
                    : workflowStep.Name;

                if (string.IsNullOrEmpty(activity.DisplayName))
                {
                    activity.DisplayName = $"step {stepName}";
                }
                else
                {
                    activity.DisplayName += $" step {stepName}";    
                }
                
                activity.SetTag("workflow.step.id", workflowStep.Id);
                activity.SetTag("workflow.step.name", stepName);
                activity.SetTag("workflow.step.type", workflowStep.BodyType?.Name);
            }
        }

        internal static void Enrich(WorkflowExecutorResult result)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag("workflow.subscriptions.count", result?.Subscriptions?.Count);
                activity.SetTag("workflow.errors.count", result?.Errors?.Count);

                if (result?.Errors?.Count > 0)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
            }
        }

        internal static void Enrich(Event evt)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.DisplayName = $"workflow process {evt?.EventName}";
                activity.SetTag("workflow.event.id", evt?.Id);
                activity.SetTag("workflow.event.name", evt?.EventName);
                activity.SetTag("workflow.event.processed", evt?.IsProcessed);
            }
        }

        internal static void EnrichWithDequeuedItem(this Activity activity, string item)
        {
            if (activity != null)
            {
                activity.SetTag("workflow.queue.item", item);
            }
        }

        private static Activity StartRootActivity(
            this ActivitySource activitySource, 
            string name, 
            ActivityKind kind)
        {
            Activity.Current = null;

            return activitySource.StartActivity(name, kind);
        }

        private static string GetQueueType(QueueType queueType)
        {
            switch (queueType)
            {
                case QueueType.Workflow: return "workflow";
                case QueueType.Event: return "event";
                case QueueType.Index: return "index";
                default: return "unknown";
            }
        }
    }
}