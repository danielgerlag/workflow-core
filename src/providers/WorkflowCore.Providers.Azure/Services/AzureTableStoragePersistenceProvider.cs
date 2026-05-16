using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class AzureTableStoragePersistenceProvider : IPersistenceProvider
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly string _workflowTableName;
        private readonly string _eventTableName;
        private readonly string _subscriptionTableName;
        private readonly string _commandTableName;
        private readonly string _errorTableName;

        private readonly Lazy<TableClient> _workflowTable;
        private readonly Lazy<TableClient> _eventTable;
        private readonly Lazy<TableClient> _subscriptionTable;
        private readonly Lazy<TableClient> _commandTable;
        private readonly Lazy<TableClient> _errorTable;

        public AzureTableStoragePersistenceProvider(
            TableServiceClient tableServiceClient,
            string tableNamePrefix = "WorkflowCore")
        {
            _tableServiceClient = tableServiceClient;
            _workflowTableName = $"{tableNamePrefix}Workflows";
            _eventTableName = $"{tableNamePrefix}Events";
            _subscriptionTableName = $"{tableNamePrefix}Subscriptions";
            _commandTableName = $"{tableNamePrefix}Commands";
            _errorTableName = $"{tableNamePrefix}Errors";

            _workflowTable = new Lazy<TableClient>(() => _tableServiceClient.GetTableClient(_workflowTableName));
            _eventTable = new Lazy<TableClient>(() => _tableServiceClient.GetTableClient(_eventTableName));
            _subscriptionTable = new Lazy<TableClient>(() => _tableServiceClient.GetTableClient(_subscriptionTableName));
            _commandTable = new Lazy<TableClient>(() => _tableServiceClient.GetTableClient(_commandTableName));
            _errorTable = new Lazy<TableClient>(() => _tableServiceClient.GetTableClient(_errorTableName));
        }

        public bool SupportsScheduledCommands => true;

        /// <summary>Escapes single quotes in OData string literals by doubling them.</summary>
        private static string EscapeOData(string value) => value?.Replace("'", "''") ?? string.Empty;

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            workflow.Id = Guid.NewGuid().ToString();
            var entity = WorkflowTableEntity.FromInstance(workflow);
            await _workflowTable.Value.AddEntityAsync(entity, cancellationToken);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            var entity = WorkflowTableEntity.FromInstance(workflow);
            await _workflowTable.Value.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
        {
            await PersistWorkflow(workflow, cancellationToken);

            foreach (var subscription in subscriptions)
            {
                await CreateEventSubscription(subscription, cancellationToken);
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken = default)
        {
            var utcTicks = asAt.ToUniversalTime().Ticks;
            var query = _workflowTable.Value.QueryAsync<WorkflowTableEntity>(
                filter: $"PartitionKey eq 'workflow' and Status eq {(int)WorkflowStatus.Runnable} and NextExecution le {utcTicks}",
                cancellationToken: cancellationToken);

            var result = new List<string>();
            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        result.Add(entity.RowKey);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return result;
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _workflowTable.Value.GetEntityAsync<WorkflowTableEntity>("workflow", id, cancellationToken: cancellationToken);
                return WorkflowTableEntity.ToInstance(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                return Enumerable.Empty<WorkflowInstance>();

            var result = new List<WorkflowInstance>();
            foreach (var id in ids)
            {
                var instance = await GetWorkflowInstance(id, cancellationToken);
                if (instance != null)
                    result.Add(instance);
            }
            return result;
        }

        [Obsolete]
        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            var filter = "PartitionKey eq 'workflow'";

            if (status.HasValue)
                filter += $" and Status eq {(int)status.Value}";

            if (!string.IsNullOrEmpty(type))
                filter += $" and WorkflowDefinitionId eq '{EscapeOData(type)}'";

            if (createdFrom.HasValue)
                filter += $" and CreateTime ge datetime'{createdFrom.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffffffZ}'";

            if (createdTo.HasValue)
                filter += $" and CreateTime le datetime'{createdTo.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffffffZ}'";

            var query = _workflowTable.Value.QueryAsync<WorkflowTableEntity>(filter: filter);
            var entities = new List<WorkflowTableEntity>();

            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        entities.Add(entity);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            return entities.Skip(skip).Take(take).Select(WorkflowTableEntity.ToInstance);
        }

        public async Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken = default)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            var entity = EventTableEntity.FromInstance(newEvent);
            await _eventTable.Value.AddEntityAsync(entity, cancellationToken);
            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _eventTable.Value.GetEntityAsync<EventTableEntity>("event", id, cancellationToken: cancellationToken);
                return EventTableEntity.ToInstance(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken = default)
        {
            var utcAsAt = asAt.ToUniversalTime();
            var query = _eventTable.Value.QueryAsync<EventTableEntity>(
                filter: $"PartitionKey eq 'event' and IsProcessed eq false and EventTime le datetime'{utcAsAt:yyyy-MM-ddTHH:mm:ss.fffffffZ}'",
                cancellationToken: cancellationToken);

            var result = new List<string>();
            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        result.Add(entity.RowKey);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return result;
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            var utcAsOf = asOf.ToUniversalTime();
            var filter = $"PartitionKey eq 'event' and EventName eq '{EscapeOData(eventName)}' and EventKey eq '{EscapeOData(eventKey)}' and EventTime ge datetime'{utcAsOf:yyyy-MM-ddTHH:mm:ss.fffffffZ}'";

            var query = _eventTable.Value.QueryAsync<EventTableEntity>(filter: filter, cancellationToken: cancellationToken);
            var result = new List<string>();

            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        result.Add(entity.RowKey);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return result;
        }

        public async Task MarkEventProcessed(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _eventTable.Value.GetEntityAsync<EventTableEntity>("event", id, cancellationToken: cancellationToken);
            entity.Value.IsProcessed = true;
            try
            {
                await _eventTable.Value.UpdateEntityAsync(entity.Value, entity.Value.ETag, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Another worker already updated this entity; it is already marked processed.
            }
        }

        public async Task MarkEventUnprocessed(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _eventTable.Value.GetEntityAsync<EventTableEntity>("event", id, cancellationToken: cancellationToken);
            entity.Value.IsProcessed = false;
            try
            {
                await _eventTable.Value.UpdateEntityAsync(entity.Value, entity.Value.ETag, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Concurrent update; the entity may already be in the desired state.
            }
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken = default)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var entity = SubscriptionTableEntity.FromInstance(subscription);
            await _subscriptionTable.Value.AddEntityAsync(entity, cancellationToken);
            return subscription.Id;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            var utcAsOf = asOf.ToUniversalTime();
            var filter = $"PartitionKey eq 'subscription' and EventName eq '{EscapeOData(eventName)}' and EventKey eq '{EscapeOData(eventKey)}' and SubscribeAsOf le datetime'{utcAsOf:yyyy-MM-ddTHH:mm:ss.fffffffZ}'";

            var query = _subscriptionTable.Value.QueryAsync<SubscriptionTableEntity>(filter: filter, cancellationToken: cancellationToken);
            var result = new List<EventSubscription>();

            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        result.Add(SubscriptionTableEntity.ToInstance(entity));
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return result;
        }

        public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            await _subscriptionTable.Value.DeleteEntityAsync("subscription", eventSubscriptionId, cancellationToken: cancellationToken);
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _subscriptionTable.Value.GetEntityAsync<SubscriptionTableEntity>("subscription", eventSubscriptionId, cancellationToken: cancellationToken);
                return SubscriptionTableEntity.ToInstance(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            var subscriptions = await GetSubscriptions(eventName, eventKey, asOf, cancellationToken);
            return subscriptions.FirstOrDefault(x => string.IsNullOrEmpty(x.ExternalToken));
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _subscriptionTable.Value.GetEntityAsync<SubscriptionTableEntity>("subscription", eventSubscriptionId, cancellationToken: cancellationToken);

                if (!string.IsNullOrEmpty(entity.Value.ExternalToken))
                    return false;

                entity.Value.ExternalToken = token;
                entity.Value.ExternalWorkerId = workerId;
                entity.Value.ExternalTokenExpiry = expiry;

                await _subscriptionTable.Value.UpdateEntityAsync(entity.Value, entity.Value.ETag, cancellationToken: cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404 || ex.Status == 412)
            {
                // 404: subscription gone; 412: another worker acquired the token concurrently.
                return false;
            }
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default)
        {
            var entity = await _subscriptionTable.Value.GetEntityAsync<SubscriptionTableEntity>("subscription", eventSubscriptionId, cancellationToken: cancellationToken);

            if (entity.Value.ExternalToken != token)
                throw new InvalidOperationException();

            entity.Value.ExternalToken = null;
            entity.Value.ExternalWorkerId = null;
            entity.Value.ExternalTokenExpiry = null;

            try
            {
                await _subscriptionTable.Value.UpdateEntityAsync(entity.Value, entity.Value.ETag, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Concurrent update; token already cleared by another worker.
            }
        }

        public async Task ScheduleCommand(ScheduledCommand command)
        {
            // Use a deterministic RowKey derived from CommandName+Data to deduplicate commands.
            // Hash to ensure the key is always a safe Azure Table Storage RowKey.
            var rowKey = ComputeCommandRowKey(command.CommandName, command.Data);
            var entity = ScheduledCommandTableEntity.FromInstance(command, rowKey);
            await _commandTable.Value.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        private static string ComputeCommandRowKey(string commandName, string data)
        {
            var input = $"{commandName}_{data}";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash).ToString("N");
            }
        }

        public async Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            var query = _commandTable.Value.QueryAsync<ScheduledCommandTableEntity>(
                filter: $"PartitionKey eq 'command' and ExecuteTime le {asOf.UtcDateTime.Ticks}",
                cancellationToken: cancellationToken);

            var pages = query.AsPages();
            var enumerator = pages.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    foreach (var entity in enumerator.Current.Values)
                    {
                        try
                        {
                            var command = ScheduledCommandTableEntity.ToInstance(entity);
                            await action(command);
                            await _commandTable.Value.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
                        }
                        catch (Exception)
                        {
                            //TODO: add logger
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default)
        {
            foreach (var error in errors)
            {
                var entity = new TableEntity("error", Guid.NewGuid().ToString())
                {
                    ["WorkflowId"] = error.WorkflowId,
                    ["ExecutionPointerId"] = error.ExecutionPointerId,
                    ["ErrorTime"] = error.ErrorTime,
                    ["Message"] = error.Message
                };

                await _errorTable.Value.AddEntityAsync(entity, cancellationToken);
            }
        }

        public void EnsureStoreExists()
        {
            _tableServiceClient.CreateTableIfNotExists(_workflowTableName);
            _tableServiceClient.CreateTableIfNotExists(_eventTableName);
            _tableServiceClient.CreateTableIfNotExists(_subscriptionTableName);
            _tableServiceClient.CreateTableIfNotExists(_commandTableName);
            _tableServiceClient.CreateTableIfNotExists(_errorTableName);
        }
    }
}