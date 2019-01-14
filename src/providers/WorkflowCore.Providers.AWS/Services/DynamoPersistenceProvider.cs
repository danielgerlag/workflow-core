﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Util;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.AWS.Services
{
    public class DynamoPersistenceProvider : IPersistenceProvider
    {
        private readonly ILogger _logger;
        private readonly AmazonDynamoDBClient _client;
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

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
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
    }
}
