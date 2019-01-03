using Amazon.DynamoDBv2;
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

        public const string WORKFLOW_TABLE = "workflows";

        public DynamoPersistenceProvider(AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<DynamoPersistenceProvider>();
            _client = new AmazonDynamoDBClient(credentials, config);
            _tablePrefix = tablePrefix;
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
                IndexName = "runnable",
                //ProjectionExpression = "id",
                KeyConditionExpression = "status = :status and nextExecution <= :effectiveDate",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":status", new AttributeValue()
                        {
                            S = WorkflowStatus.Runnable.ToString()
                        }
                    },
                    {
                        ":effectiveDate", new AttributeValue()
                        {
                            N = Convert.ToString(now)
                        }
                    }
                },
                Select = "ALL_PROJECTED_ATTRIBUTES",
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

        public Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public Task TerminateSubscription(string eventSubscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateEvent(Event newEvent)
        {
            throw new NotImplementedException();
        }

        public Task<Event> GetEvent(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var result = new List<string>();
            //TODO
            return result;
        }

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public Task MarkEventProcessed(string id)
        {
            throw new NotImplementedException();
        }

        public Task MarkEventUnprocessed(string id)
        {
            throw new NotImplementedException();
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            throw new NotImplementedException();
        }

        public void EnsureStoreExists()
        {
            throw new NotImplementedException();
        }
    }
}
