using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Providers.AWS.Interface;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.AWS.Services
{
    public class DynamoPersistenceProvider : IPersistenceProvider
    {
        private readonly ILogger _logger;
        private readonly IAmazonDynamoDB _client;
        private readonly string _tablePrefix;
        private readonly IDynamoDbProvisioner _provisioner;

        public const string WORKFLOW_TABLE = "workflows";
        public const string SUBCRIPTION_TABLE = "subscriptions";
        public const string EVENT_TABLE = "events";

        public DynamoPersistenceProvider(AWSCredentials credentials, AmazonDynamoDBConfig config, IDynamoDbProvisioner provisioner, string tablePrefix, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<DynamoPersistenceProvider>();
            _client = new AmazonDynamoDBClient(credentials, config);
            _tablePrefix = tablePrefix;
            _provisioner = provisioner;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();

            var req = new PutItemRequest()
            {
                TableName = $"{_tablePrefix}-{WORKFLOW_TABLE}",
                Item = workflow.ToDynamoMap(),
                ConditionExpression = "attribute_not_exists(id)"
            };

            var response = await _client.PutItemAsync(req);

            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            var request = new PutItemRequest()
            {
                TableName = $"{_tablePrefix}-{WORKFLOW_TABLE}",
                Item = workflow.ToDynamoMap()
            };

            var response = await _client.PutItemAsync(request);
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            var result = new List<string>();
            var now = asAt.ToUniversalTime().Ticks;

            var request = new QueryRequest()
            {
                TableName = $"{_tablePrefix}-{WORKFLOW_TABLE}",
                IndexName = "ix_runnable",
                ProjectionExpression = "id",
                KeyConditionExpression = "runnable = :r and next_execution <= :effective_date",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":r", new AttributeValue()
                        {
                            N = 1.ToString()
                        }
                    },
                    {
                        ":effective_date", new AttributeValue()
                        {
                            N = Convert.ToString(now)
                        }
                    }
                },
                ScanIndexForward = true
            };

            var response = await _client.QueryAsync(request);

            foreach (var item in response.Items)
            {
                result.Add(item["id"].S);
            }

            return result;
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            var req = new GetItemRequest()
            {
                TableName = $"{_tablePrefix}-{WORKFLOW_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(Id) }
                }
            };
            var response = await _client.GetItemAsync(req);

            return response.Item.ToWorkflowInstance();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            var keys = new KeysAndAttributes() { Keys = new List<Dictionary<string, AttributeValue>>() };
            foreach (var id in ids)
            {
                var key = new Dictionary<string, AttributeValue>()
                {
                    {
                        "id", new AttributeValue { S = id }
                    }
                };
                keys.Keys.Add(key);
            }

            var request = new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>()
                {
                    {
                        $"{_tablePrefix}-{WORKFLOW_TABLE}", keys
                    }
                }
            };

            var result = new List<Dictionary<string, AttributeValue>>();
            BatchGetItemResponse response;
            do
            {
                response = await _client.BatchGetItemAsync(request);
                foreach (var tableResponse in response.Responses)
                    result.AddRange(tableResponse.Value);

                request.RequestItems = response.UnprocessedKeys;
            } while (response.UnprocessedKeys.Count > 0);

            return result.Select(i => i.ToWorkflowInstance());
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();

            var req = new PutItemRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                Item = subscription.ToDynamoMap(),
                ConditionExpression = "attribute_not_exists(id)"
            };

            var response = await _client.PutItemAsync(req);

            return subscription.Id;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            var result = new List<EventSubscription>();
            var asOfTicks = asOf.ToUniversalTime().Ticks;

            var request = new QueryRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                IndexName = "ix_slug",
                Select = "ALL_PROJECTED_ATTRIBUTES",
                KeyConditionExpression = "event_slug = :slug and subscribe_as_of <= :as_of",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":slug", new AttributeValue($"{eventName}:{eventKey}")
                    },
                    {
                        ":as_of", new AttributeValue()
                        {
                            N = Convert.ToString(asOfTicks)
                        }
                    }
                },
                ScanIndexForward = true
            };

            var response = await _client.QueryAsync(request);

            foreach (var item in response.Items)
            {
                result.Add(item.ToEventSubscription());
            }

            return result;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            var request = new DeleteItemRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(eventSubscriptionId) }
                }
            };
            await _client.DeleteItemAsync(request);
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            newEvent.Id = Guid.NewGuid().ToString();

            var req = new PutItemRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                Item = newEvent.ToDynamoMap(),
                ConditionExpression = "attribute_not_exists(id)"
            };

            var response = await _client.PutItemAsync(req);

            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id)
        {
            var req = new GetItemRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(id) }
                }
            };
            var response = await _client.GetItemAsync(req);

            return response.Item.ToEvent();
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var result = new List<string>();
            var now = asAt.ToUniversalTime().Ticks;

            var request = new QueryRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                IndexName = "ix_not_processed",
                ProjectionExpression = "id",
                KeyConditionExpression = "not_processed = :n and event_time <= :effectiveDate",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":n" , new AttributeValue() { N = 1.ToString() } },
                    {
                        ":effectiveDate", new AttributeValue()
                        {
                            N = Convert.ToString(now)
                        }
                    }
                },
                ScanIndexForward = true
            };

            var response = await _client.QueryAsync(request);

            foreach (var item in response.Items)
            {
                result.Add(item["id"].S);
            }

            return result;
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            var result = new List<string>();
            var asOfTicks = asOf.ToUniversalTime().Ticks;

            var request = new QueryRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                IndexName = "ix_slug",
                ProjectionExpression = "id",
                KeyConditionExpression = "event_slug = :slug and event_time >= :effective_date",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":slug", new AttributeValue($"{eventName}:{eventKey}")
                    },
                    {
                        ":effective_date", new AttributeValue()
                        {
                            N = Convert.ToString(asOfTicks)
                        }
                    }
                },
                ScanIndexForward = true
            };

            var response = await _client.QueryAsync(request);

            foreach (var item in response.Items)
            {
                result.Add(item["id"].S);
            }

            return result;
        }

        public async Task MarkEventProcessed(string id)
        {
            var request = new UpdateItemRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(id) }
                },
                UpdateExpression = "REMOVE not_processed"
            };
            await _client.UpdateItemAsync(request);
        }

        public async Task MarkEventUnprocessed(string id)
        {
            var request = new UpdateItemRequest()
            {
                TableName = $"{_tablePrefix}-{EVENT_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(id) }
                },
                UpdateExpression = "ADD not_processed = :n",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":n" , new AttributeValue() { N = 1.ToString() } }
                }
            };
            await _client.UpdateItemAsync(request);
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            //TODO
            return Task.CompletedTask;
        }

        public void EnsureStoreExists()
        {
            _provisioner.ProvisionTables().Wait();
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            var req = new GetItemRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(eventSubscriptionId) }
                }
            };
            var response = await _client.GetItemAsync(req);

            return response.Item.ToEventSubscription();
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            var result = new List<EventSubscription>();
            var asOfTicks = asOf.ToUniversalTime().Ticks;

            var request = new QueryRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                IndexName = "ix_slug",
                Select = "ALL_PROJECTED_ATTRIBUTES",
                KeyConditionExpression = "event_slug = :slug and subscribe_as_of <= :as_of",
                FilterExpression = "attribute_not_exists(external_token)",
                Limit = 1,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":slug", new AttributeValue($"{eventName}:{eventKey}")
                    },
                    {
                        ":as_of", new AttributeValue()
                        {
                            N = Convert.ToString(asOfTicks)
                        }
                    }
                },
                ScanIndexForward = true
            };

            var response = await _client.QueryAsync(request);

            foreach (var item in response.Items)
                result.Add(item.ToEventSubscription());

            return result.FirstOrDefault();
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            var request = new UpdateItemRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(eventSubscriptionId) }
                },
                UpdateExpression = "SET external_token = :external_token, external_worker_id = :external_worker_id, external_token_expiry = :external_token_expiry",
                ConditionExpression = "attribute_not_exists(external_token)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":external_token" , new AttributeValue() { S = token } },
                    { ":external_worker_id" , new AttributeValue() { S = workerId } },
                    { ":external_token_expiry" , new AttributeValue() { N = expiry.Ticks.ToString() } }
                }
            };
            try
            {
                await _client.UpdateItemAsync(request);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            var request = new UpdateItemRequest()
            {
                TableName = $"{_tablePrefix}-{SUBCRIPTION_TABLE}",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(eventSubscriptionId) }
                },
                UpdateExpression = "REMOVE external_token, external_worker_id, external_token_expiry",
                ConditionExpression = "external_token = :external_token",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":external_token" , new AttributeValue() { S = token } },
                }
            };
            
            await _client.UpdateItemAsync(request);
        }
    }
}