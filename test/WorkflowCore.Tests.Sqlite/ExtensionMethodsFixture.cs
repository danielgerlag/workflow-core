using System;
using FluentAssertions;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Models;
using Xunit;

namespace WorkflowCore.Tests.Sqlite
{
    public class ExtensionMethodsFixture
    {
        [Fact]
        public void ToWorkflowInstance_CreateTime_Local_Kind_Should_Convert_To_Utc()
        {
            var localTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            var persisted = BuildPersistedWorkflow(createTime: localTime);

            var instance = persisted.ToWorkflowInstance();

            instance.CreateTime.Should().Be(expectedUtc);
            instance.CreateTime.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ToWorkflowInstance_CreateTime_Utc_Kind_Should_Return_Same_Value()
        {
            var utcTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var persisted = BuildPersistedWorkflow(createTime: utcTime);

            var instance = persisted.ToWorkflowInstance();

            instance.CreateTime.Should().Be(utcTime);
            instance.CreateTime.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ToWorkflowInstance_CreateTime_Unspecified_Kind_Should_Treat_As_Utc()
        {
            var unspecifiedTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

            var persisted = BuildPersistedWorkflow(createTime: unspecifiedTime);

            var instance = persisted.ToWorkflowInstance();

            instance.CreateTime.Should().Be(DateTime.SpecifyKind(unspecifiedTime, DateTimeKind.Utc));
            instance.CreateTime.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ToWorkflowInstance_SleepUntil_Local_Kind_Should_Convert_To_Utc()
        {
            var localTime = new DateTime(2026, 3, 26, 12, 30, 31, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            var persisted = BuildPersistedWorkflow();
            persisted.ExecutionPointers.Add(new PersistedExecutionPointer
            {
                Id = "ep1",
                StepId = 0,
                Active = true,
                SleepUntil = localTime,
                PersistenceData = "null",
                ContextItem = "null",
                Scope = "",
                Children = "",
                EventData = "null",
                Outcome = "null"
            });

            var instance = persisted.ToWorkflowInstance();

            var pointer = instance.ExecutionPointers.FindById("ep1");
            pointer.SleepUntil.Should().Be(expectedUtc);
            pointer.SleepUntil.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        private static PersistedWorkflow BuildPersistedWorkflow(DateTime? createTime = null)
        {
            return new PersistedWorkflow
            {
                InstanceId = Guid.NewGuid(),
                WorkflowDefinitionId = "test",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                Data = "{}",
                CreateTime = createTime ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }
    }
}
