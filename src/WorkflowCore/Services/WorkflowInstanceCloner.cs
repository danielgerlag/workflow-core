using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowInstanceCloner : IWorkflowInstanceCloner
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private readonly IDateTimeProvider _dateTimeProvider;

        public WorkflowInstanceCloner(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public (WorkflowInstance clone, List<EventSubscription> subscriptions) CloneForFork(
            WorkflowInstance source,
            Action<object> dataMutator = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var activePointers = source.ExecutionPointers
                .Where(p => IsActiveOrPending(p))
                .ToList();

            var requiredPointerIds = new HashSet<string>();
            foreach (var pointer in activePointers)
            {
                requiredPointerIds.Add(pointer.Id);
                foreach (var scopeId in pointer.Scope)
                {
                    requiredPointerIds.Add(scopeId);
                }
                // Include all children of active pointers so IsBranchComplete works correctly
                foreach (var childId in pointer.Children)
                {
                    requiredPointerIds.Add(childId);
                    IncludeChildrenRecursive(source, childId, requiredPointerIds);
                }
            }

            var pointersToClone = source.ExecutionPointers
                .Where(p => requiredPointerIds.Contains(p.Id))
                .ToList();

            var idMapping = new Dictionary<string, string>();
            foreach (var pointer in pointersToClone)
            {
                idMapping[pointer.Id] = Guid.NewGuid().ToString();
            }

            var clonedPointers = new ExecutionPointerCollection(pointersToClone.Count);
            foreach (var pointer in pointersToClone)
            {
                var cloned = ClonePointer(pointer, idMapping);
                clonedPointers.Add(cloned);
            }

            var clonedData = DeepClone(source.Data);
            dataMutator?.Invoke(clonedData);

            var now = _dateTimeProvider.UtcNow;

            var clone = new WorkflowInstance
            {
                WorkflowDefinitionId = source.WorkflowDefinitionId,
                Version = source.Version,
                Description = source.Description,
                Reference = source.Reference,
                NextExecution = 0,
                Status = WorkflowStatus.Runnable,
                Data = clonedData,
                CreateTime = now,
                CompleteTime = null,
                ExecutionPointers = clonedPointers
            };

            var subscriptions = new List<EventSubscription>();
            foreach (var pointer in clonedPointers)
            {
                if (pointer.Status == PointerStatus.WaitingForEvent &&
                    !string.IsNullOrEmpty(pointer.EventName))
                {
                    subscriptions.Add(new EventSubscription
                    {
                        StepId = pointer.StepId,
                        ExecutionPointerId = pointer.Id,
                        EventName = pointer.EventName,
                        EventKey = pointer.EventKey,
                        SubscribeAsOf = now
                    });
                }
            }

            return (clone, subscriptions);
        }

        private static bool IsActiveOrPending(ExecutionPointer pointer)
        {
            if (pointer.Status == PointerStatus.Legacy)
                return pointer.Active;

            return pointer.Status == PointerStatus.Pending
                || pointer.Status == PointerStatus.Running
                || pointer.Status == PointerStatus.Sleeping
                || pointer.Status == PointerStatus.WaitingForEvent
                || pointer.Status == PointerStatus.PendingPredecessor;
        }

        private static void IncludeChildrenRecursive(WorkflowInstance source, string pointerId, HashSet<string> requiredIds)
        {
            var pointer = source.ExecutionPointers.FindById(pointerId);
            if (pointer == null)
                return;

            foreach (var childId in pointer.Children)
            {
                if (requiredIds.Add(childId))
                {
                    IncludeChildrenRecursive(source, childId, requiredIds);
                }
            }
        }

        private static ExecutionPointer ClonePointer(ExecutionPointer source, Dictionary<string, string> idMapping)
        {
            var newId = idMapping[source.Id];

            string newPredecessorId = null;
            if (source.PredecessorId != null && idMapping.TryGetValue(source.PredecessorId, out var mappedPred))
            {
                newPredecessorId = mappedPred;
            }

            var newChildren = new List<string>();
            foreach (var childId in source.Children)
            {
                if (idMapping.TryGetValue(childId, out var mappedChild))
                    newChildren.Add(mappedChild);
            }

            var newScope = new List<string>();
            foreach (var scopeId in source.Scope)
            {
                if (idMapping.TryGetValue(scopeId, out var mappedScope))
                    newScope.Add(mappedScope);
            }

            return new ExecutionPointer
            {
                Id = newId,
                StepId = source.StepId,
                Active = source.Active,
                SleepUntil = source.SleepUntil,
                PersistenceData = DeepClone(source.PersistenceData),
                StartTime = source.StartTime,
                EndTime = source.EndTime,
                EventName = source.EventName,
                EventKey = source.EventKey,
                EventPublished = source.EventPublished,
                EventData = DeepClone(source.EventData),
                ExtensionAttributes = source.ExtensionAttributes != null
                    ? source.ExtensionAttributes.ToDictionary(
                        kvp => kvp.Key, kvp => DeepClone(kvp.Value))
                    : new Dictionary<string, object>(),
                StepName = source.StepName,
                RetryCount = source.RetryCount,
                Children = newChildren,
                ContextItem = DeepClone(source.ContextItem),
                PredecessorId = newPredecessorId,
                Outcome = DeepClone(source.Outcome),
                Status = source.Status,
                Scope = newScope
            };
        }

        private static object DeepClone(object source)
        {
            if (source == null)
                return null;

            var type = source.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime) || type == typeof(Guid))
            {
                return source;
            }

            var json = JsonConvert.SerializeObject(source, _jsonSettings);
            return JsonConvert.DeserializeObject(json, source.GetType(), _jsonSettings);
        }
    }
}
