using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using FluentAssertions;
using Xunit;

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowInstanceClonerFixture
    {
        protected IWorkflowInstanceCloner Subject;
        protected IDateTimeProvider DateTimeProvider;

        public WorkflowInstanceClonerFixture()
        {
            DateTimeProvider = A.Fake<IDateTimeProvider>();
            A.CallTo(() => DateTimeProvider.UtcNow).Returns(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
            Subject = new WorkflowInstanceCloner(DateTimeProvider);
        }

        [Fact(DisplayName = "Should clone only active pointer")]
        public void should_clone_only_active_pointer()
        {
            var activePointer = BuildPointer("4", 4, PointerStatus.Running);
            var source = BuildWorkflow(
                new TestData { Quantity = 1, Name = "source" },
                BuildPointer("1", 1, PointerStatus.Complete),
                BuildPointer("2", 2, PointerStatus.Complete),
                BuildPointer("3", 3, PointerStatus.Complete),
                activePointer);

            var (clone, subscriptions) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(1);
            clone.ExecutionPointers.Single().StepId.Should().Be(activePointer.StepId);
            subscriptions.Should().BeEmpty();
        }

        [Fact(DisplayName = "Should generate new pointer ids")]
        public void should_generate_new_pointer_ids()
        {
            var source = BuildWorkflow(
                null,
                BuildPointer("1", 1, PointerStatus.Pending),
                BuildPointer("2", 2, PointerStatus.Running));

            var (clone, _) = Subject.CloneForFork(source);
            var sourceIds = source.ExecutionPointers.Select(x => x.Id).ToHashSet();
            var cloneIds = clone.ExecutionPointers.Select(x => x.Id).ToList();

            cloneIds.Should().OnlyContain(id => !sourceIds.Contains(id));
            cloneIds.All(IsGuid).Should().BeTrue();
        }

        [Fact(DisplayName = "Should include scope chain ancestors")]
        public void should_include_scope_chain_ancestors()
        {
            var rootPointer = BuildPointer("root", 1, PointerStatus.Complete);
            var scopePointer = BuildPointer("scope", 2, PointerStatus.Complete, scope: new[] { rootPointer.Id });
            var activePointer = BuildPointer("active", 3, PointerStatus.Running, scope: new[] { rootPointer.Id, scopePointer.Id });
            var source = BuildWorkflow(null, rootPointer, scopePointer, activePointer);

            var (clone, _) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(3);
            clone.ExecutionPointers.Select(x => x.StepId).Should().BeEquivalentTo(new[] { 1, 2, 3 });

            // Verify scope IDs are correctly remapped to cloned pointer IDs
            var clonedActive = FindPointer(clone, 3);
            var clonedRoot = FindPointer(clone, 1);
            var clonedScope = FindPointer(clone, 2);
            var scopeList = clonedActive.Scope.ToList();
            scopeList.Should().HaveCount(2);
            scopeList[0].Should().Be(clonedRoot.Id);
            scopeList[1].Should().Be(clonedScope.Id);
        }

        [Fact(DisplayName = "Should remap predecessor id")]
        public void should_remap_predecessor_id()
        {
            var parentPointer = BuildPointer("parent", 1, PointerStatus.Pending);
            var childPointer = BuildPointer("child", 2, PointerStatus.Running, predecessorId: parentPointer.Id);
            var source = BuildWorkflow(null, parentPointer, childPointer);

            var (clone, _) = Subject.CloneForFork(source);
            var clonedParent = FindPointer(clone, parentPointer.StepId);
            var clonedChild = FindPointer(clone, childPointer.StepId);

            clonedChild.PredecessorId.Should().Be(clonedParent.Id);
            clonedChild.PredecessorId.Should().NotBe(parentPointer.Id);
        }

        [Fact(DisplayName = "Should remap children ids")]
        public void should_remap_children_ids()
        {
            var childPointer1 = BuildPointer("child-1", 2, PointerStatus.Pending);
            var childPointer2 = BuildPointer("child-2", 3, PointerStatus.Running);
            var parentPointer = BuildPointer("parent", 1, PointerStatus.Running, children: new[] { childPointer1.Id, childPointer2.Id });
            var source = BuildWorkflow(null, parentPointer, childPointer1, childPointer2);

            var (clone, _) = Subject.CloneForFork(source);
            var clonedParent = FindPointer(clone, parentPointer.StepId);
            var expectedChildIds = clone.ExecutionPointers
                .Where(x => x.StepId == childPointer1.StepId || x.StepId == childPointer2.StepId)
                .Select(x => x.Id)
                .ToList();

            clonedParent.Children.Should().BeEquivalentTo(expectedChildIds);
            clonedParent.Children.Should().NotContain(childPointer1.Id);
            clonedParent.Children.Should().NotContain(childPointer2.Id);
        }

        [Fact(DisplayName = "Should deep clone data")]
        public void should_deep_clone_data()
        {
            var sourceData = new TestData { Quantity = 3, Name = "source" };
            var source = BuildWorkflow(sourceData, BuildPointer("1", 1, PointerStatus.Running));

            var (clone, _) = Subject.CloneForFork(source);
            var clonedData = (TestData)clone.Data;

            clonedData.Quantity = 10;
            clonedData.Name = "changed";

            ((TestData)source.Data).Quantity.Should().Be(3);
            ((TestData)source.Data).Name.Should().Be("source");
            clone.Data.Should().NotBeSameAs(source.Data);
        }

        [Fact(DisplayName = "Should apply data mutation callback")]
        public void should_apply_data_mutation_callback()
        {
            var source = BuildWorkflow(new TestData { Quantity = 2, Name = "source" }, BuildPointer("1", 1, PointerStatus.Running));

            var (clone, _) = Subject.CloneForFork(source, data =>
            {
                var clonedData = (TestData)data;
                clonedData.Quantity = 7;
                clonedData.Name = "mutated";
            });

            ((TestData)clone.Data).Quantity.Should().Be(7);
            ((TestData)clone.Data).Name.Should().Be("mutated");
            ((TestData)source.Data).Quantity.Should().Be(2);
            ((TestData)source.Data).Name.Should().Be("source");
        }

        [Fact(DisplayName = "Should work without mutation callback")]
        public void should_work_without_mutation_callback()
        {
            var source = BuildWorkflow(new TestData { Quantity = 4, Name = "source" }, BuildPointer("1", 1, PointerStatus.Running));

            Action act = () => Subject.CloneForFork(source, null);

            act.ShouldNotThrow();
            var (clone, _) = Subject.CloneForFork(source, null);
            ((TestData)clone.Data).ShouldBeEquivalentTo((TestData)source.Data);
            clone.Data.Should().NotBeSameAs(source.Data);
        }

        [Fact(DisplayName = "Should create event subscription for waiting pointer")]
        public void should_create_event_subscription_for_waiting_pointer()
        {
            var waitingPointer = BuildPointer("1", 1, PointerStatus.WaitingForEvent, eventName: "event-name", eventKey: "event-key");
            var source = BuildWorkflow(null, waitingPointer);

            var (clone, subscriptions) = Subject.CloneForFork(source);
            var clonedPointer = FindPointer(clone, waitingPointer.StepId);
            var subscription = subscriptions.Should().ContainSingle().Which;

            subscription.ExecutionPointerId.Should().Be(clonedPointer.Id);
            subscription.ExecutionPointerId.Should().NotBe(waitingPointer.Id);
            subscription.EventName.Should().Be("event-name");
            subscription.EventKey.Should().Be("event-key");
            subscription.StepId.Should().Be(waitingPointer.StepId);
        }

        [Fact(DisplayName = "Should clone multiple active pointers")]
        public void should_clone_multiple_active_pointers()
        {
            var pointer1 = BuildPointer("1", 1, PointerStatus.Pending);
            var pointer2 = BuildPointer("2", 2, PointerStatus.Sleeping);
            var completedPointer = BuildPointer("3", 3, PointerStatus.Complete);
            var source = BuildWorkflow(null, pointer1, pointer2, completedPointer);

            var (clone, _) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(2);
            clone.ExecutionPointers.Select(x => x.StepId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact(DisplayName = "Should deep clone extension attributes")]
        public void should_deep_clone_extension_attributes()
        {
            var sourcePointer = BuildPointer(
                "1",
                1,
                PointerStatus.Running,
                extensionAttributes: new Dictionary<string, object>
                {
                    ["data"] = new TestData { Quantity = 5, Name = "source" }
                });
            var source = BuildWorkflow(null, sourcePointer);

            var (clone, _) = Subject.CloneForFork(source);
            var clonedPointer = FindPointer(clone, sourcePointer.StepId);
            var clonedAttribute = (TestData)clonedPointer.ExtensionAttributes["data"];

            clonedAttribute.Quantity = 11;
            clonedAttribute.Name = "changed";

            ((TestData)sourcePointer.ExtensionAttributes["data"]).Quantity.Should().Be(5);
            ((TestData)sourcePointer.ExtensionAttributes["data"]).Name.Should().Be("source");
            clonedPointer.ExtensionAttributes.Should().NotBeSameAs(sourcePointer.ExtensionAttributes);
        }

        [Fact(DisplayName = "Should deep clone context item")]
        public void should_deep_clone_context_item()
        {
            var sourcePointer = BuildPointer("1", 1, PointerStatus.Running, contextItem: new TestData { Quantity = 6, Name = "source" });
            var source = BuildWorkflow(null, sourcePointer);

            var (clone, _) = Subject.CloneForFork(source);
            var clonedPointer = FindPointer(clone, sourcePointer.StepId);
            var clonedContextItem = (TestData)clonedPointer.ContextItem;

            clonedContextItem.Quantity = 12;
            clonedContextItem.Name = "changed";

            ((TestData)sourcePointer.ContextItem).Quantity.Should().Be(6);
            ((TestData)sourcePointer.ContextItem).Name.Should().Be("source");
            clonedPointer.ContextItem.Should().NotBeSameAs(sourcePointer.ContextItem);
        }

        [Fact(DisplayName = "Should deep clone persistence data")]
        public void should_deep_clone_persistence_data()
        {
            var sourcePointer = BuildPointer("1", 1, PointerStatus.Running, persistenceData: new TestData { Quantity = 8, Name = "source" });
            var source = BuildWorkflow(null, sourcePointer);

            var (clone, _) = Subject.CloneForFork(source);
            var clonedPointer = FindPointer(clone, sourcePointer.StepId);
            var clonedPersistenceData = (TestData)clonedPointer.PersistenceData;

            clonedPersistenceData.Quantity = 13;
            clonedPersistenceData.Name = "changed";

            ((TestData)sourcePointer.PersistenceData).Quantity.Should().Be(8);
            ((TestData)sourcePointer.PersistenceData).Name.Should().Be("source");
            clonedPointer.PersistenceData.Should().NotBeSameAs(sourcePointer.PersistenceData);
        }

        [Fact(DisplayName = "Should handle null workflow data")]
        public void should_handle_null_workflow_data()
        {
            var source = BuildWorkflow(null, BuildPointer("1", 1, PointerStatus.Running));

            var (clone, _) = Subject.CloneForFork(source);

            clone.Data.Should().BeNull();
            clone.ExecutionPointers.Should().HaveCount(1);
        }

        [Fact(DisplayName = "Should return empty pointers when all are complete")]
        public void should_return_empty_pointers_when_all_are_complete()
        {
            var source = BuildWorkflow(
                new TestData { Quantity = 1, Name = "source" },
                BuildPointer("1", 1, PointerStatus.Complete),
                BuildPointer("2", 2, PointerStatus.Complete),
                BuildPointer("3", 3, PointerStatus.Complete));

            var (clone, subscriptions) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().BeEmpty();
            subscriptions.Should().BeEmpty();
        }

        [Fact(DisplayName = "Should set excluded predecessor id to null")]
        public void should_set_excluded_predecessor_id_to_null()
        {
            var completedPred = BuildPointer("pred", 1, PointerStatus.Complete);
            var activePointer = BuildPointer("active", 2, PointerStatus.Running, predecessorId: completedPred.Id);
            var source = BuildWorkflow(null, completedPred, activePointer);

            var (clone, _) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(1);
            var clonedActive = FindPointer(clone, 2);
            clonedActive.PredecessorId.Should().BeNull();
        }

        [Fact(DisplayName = "Should include Legacy status pointers when Active flag is true")]
        public void should_include_legacy_status_pointers_when_active()
        {
            var legacyActive = BuildPointer("legacy-active", 1, PointerStatus.Legacy, active: true);
            var legacyInactive = BuildPointer("legacy-inactive", 2, PointerStatus.Legacy, active: false);
            var source = BuildWorkflow(null, legacyActive, legacyInactive);

            var (clone, _) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(1);
            clone.ExecutionPointers.Single().StepId.Should().Be(1);
        }

        [Fact(DisplayName = "Should include children of active pointers even if complete")]
        public void should_include_children_of_active_pointers_even_if_complete()
        {
            var child1 = BuildPointer("child-1", 2, PointerStatus.Complete);
            var child2 = BuildPointer("child-2", 3, PointerStatus.Complete);
            var parentPointer = BuildPointer("parent", 1, PointerStatus.Running, children: new[] { child1.Id, child2.Id });
            var source = BuildWorkflow(null, parentPointer, child1, child2);

            var (clone, _) = Subject.CloneForFork(source);

            clone.ExecutionPointers.Should().HaveCount(3);
            clone.ExecutionPointers.Select(x => x.StepId).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact(DisplayName = "Should force forked instance to Runnable status")]
        public void should_force_forked_instance_to_runnable()
        {
            var source = BuildWorkflow(null, BuildPointer("1", 1, PointerStatus.Running));
            source.Status = WorkflowStatus.Suspended;

            var (clone, _) = Subject.CloneForFork(source);

            clone.Status.Should().Be(WorkflowStatus.Runnable);
        }

        [Fact(DisplayName = "Should use IDateTimeProvider for CreateTime and SubscribeAsOf")]
        public void should_use_datetime_provider()
        {
            var fixedTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            A.CallTo(() => DateTimeProvider.UtcNow).Returns(fixedTime);

            var waitingPointer = BuildPointer("1", 1, PointerStatus.WaitingForEvent, eventName: "evt", eventKey: "key");
            var source = BuildWorkflow(null, waitingPointer);

            var (clone, subscriptions) = Subject.CloneForFork(source);

            clone.CreateTime.Should().Be(fixedTime);
            subscriptions.Single().SubscribeAsOf.Should().Be(fixedTime);
        }

        private static WorkflowInstance BuildWorkflow(object data, params ExecutionPointer[] pointers)
        {
            return new WorkflowInstance
            {
                Id = "workflow-id",
                WorkflowDefinitionId = "workflow-definition",
                Version = 1,
                Description = "workflow",
                Reference = "reference",
                Status = WorkflowStatus.Runnable,
                Data = data,
                CreateTime = DateTime.UtcNow,
                CompleteTime = DateTime.UtcNow,
                NextExecution = 42,
                ExecutionPointers = new ExecutionPointerCollection(pointers.ToList())
            };
        }

        private static ExecutionPointer BuildPointer(
            string id,
            int stepId,
            PointerStatus status,
            IEnumerable<string> scope = null,
            IEnumerable<string> children = null,
            string predecessorId = null,
            string eventName = null,
            string eventKey = null,
            object persistenceData = null,
            object contextItem = null,
            Dictionary<string, object> extensionAttributes = null,
            bool? active = null)
        {
            return new ExecutionPointer
            {
                Id = id,
                StepId = stepId,
                Active = active ?? (status != PointerStatus.Complete),
                Status = status,
                Scope = (scope ?? Enumerable.Empty<string>()).ToList(),
                Children = (children ?? Enumerable.Empty<string>()).ToList(),
                PredecessorId = predecessorId,
                EventName = eventName,
                EventKey = eventKey,
                PersistenceData = persistenceData,
                ContextItem = contextItem,
                ExtensionAttributes = extensionAttributes ?? new Dictionary<string, object>()
            };
        }

        private static ExecutionPointer FindPointer(WorkflowInstance workflow, int stepId)
        {
            return workflow.ExecutionPointers.Single(x => x.StepId == stepId);
        }

        private static bool IsGuid(string value)
        {
            Guid parsed;
            return Guid.TryParse(value, out parsed);
        }

        public class TestData
        {
            public int Quantity { get; set; }
            public string Name { get; set; }
        }
    }
}
