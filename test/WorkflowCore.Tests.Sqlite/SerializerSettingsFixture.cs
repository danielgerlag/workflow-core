using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Sqlite;
using Xunit;

namespace WorkflowCore.Tests.Sqlite
{
    [Collection("Sqlite collection")]
    public class SerializerSettingsFixture
    {
        private readonly string _connectionString;

        public SerializerSettingsFixture(SqliteSetup setup)
        {
            _connectionString = setup.ConnectionString;
        }

        [Fact]
        public void Default_settings_should_serialize_identically_to_previous_settings()
        {
            var previousSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var data = new AllowedData { Value = 7 };

            var json = JsonConvert.SerializeObject(data, ExtensionMethods.CreateSerializerSettings());

            json.Should().Be(JsonConvert.SerializeObject(data, previousSettings));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Configured_binder_should_round_trip_allowed_type(bool largeDataOptimized)
        {
            var subject = BuildProvider(s => s.SerializationBinder = new AllowListBinder(), largeDataOptimized);
            var workflowId = await subject.CreateNewWorkflow(BuildWorkflow());

            var retrieved = await subject.GetWorkflowInstance(workflowId);

            retrieved.Data.Should().BeOfType<AllowedData>().Which.Value.Should().Be(7);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Configured_binder_should_reject_disallowed_type(bool largeDataOptimized)
        {
            var subject = BuildProvider(s => s.SerializationBinder = new AllowListBinder(), largeDataOptimized);
            var workflowId = await subject.CreateNewWorkflow(BuildWorkflow());
            OverwriteData(workflowId, new DisallowedData());

            await Assert.ThrowsAsync<JsonSerializationException>(() => subject.GetWorkflowInstance(workflowId));
        }

        [Fact]
        public async Task Default_settings_should_not_restrict_types()
        {
            var subject = BuildProvider(null, false);
            var workflowId = await subject.CreateNewWorkflow(BuildWorkflow());
            OverwriteData(workflowId, new DisallowedData());

            var retrieved = await subject.GetWorkflowInstance(workflowId);

            retrieved.Data.Should().BeOfType<DisallowedData>();
        }

        // Calls go through IPersistenceProvider because LargeDataOptimizedEntityFrameworkPersistenceProvider hides the base methods with 'new'
        private IPersistenceProvider BuildProvider(Action<JsonSerializerSettings> configureSerializer, bool largeDataOptimized)
        {
            var contextFactory = new SqliteContextFactory(_connectionString);
            IPersistenceProvider provider = largeDataOptimized
                ? new LargeDataOptimizedEntityFrameworkPersistenceProvider(contextFactory, true, false, configureSerializer)
                : new EntityFrameworkPersistenceProvider(contextFactory, true, false, configureSerializer);
            provider.EnsureStoreExists();
            return provider;
        }

        private void OverwriteData(string workflowId, object data)
        {
            using (var db = new SqliteContextFactory(_connectionString).Build())
            {
                var persisted = db.Set<PersistedWorkflow>().AsTracking().Single(x => x.InstanceId == new Guid(workflowId));
                persisted.Data = JsonConvert.SerializeObject(data, ExtensionMethods.CreateSerializerSettings());
                db.SaveChanges();
            }
        }

        private static WorkflowInstance BuildWorkflow() => new WorkflowInstance
        {
            Data = new AllowedData { Value = 7 },
            Status = WorkflowStatus.Runnable,
            Version = 1,
            WorkflowDefinitionId = "serializer-settings"
        };

        public class AllowedData
        {
            public int Value { get; set; }
        }

        public class DisallowedData
        {
        }

        private class AllowListBinder : DefaultSerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var type = base.BindToType(assemblyName, typeName);
                if (type != typeof(AllowedData))
                    throw new JsonSerializationException($"Type '{typeName}' is not allowed.");

                return type;
            }
        }
    }
}
