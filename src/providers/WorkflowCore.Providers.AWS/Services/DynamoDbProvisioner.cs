using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class DynamoDbProvisioner
    {
        private readonly ILogger _logger;
        private readonly AmazonDynamoDBClient _client;
        private readonly string _tablePrefix;

        public DynamoDbProvisioner(AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<DynamoDbProvisioner>();
            _client = new AmazonDynamoDBClient(credentials, config);
            _tablePrefix = tablePrefix;
        }

        public async Task ProvisionTables()
        {
            await EnsureTable($"{_tablePrefix}-{DynamoPersistenceProvider.WORKFLOW_TABLE}", async () => await CreateWorkflowTable());
        }

        private async Task CreateWorkflowTable()
        {
            var runnableIndex = new GlobalSecondaryIndex()
            {
                IndexName = "runnable",
                KeySchema = new List<KeySchemaElement>()
                {
                    {
                        new KeySchemaElement
                        {
                            AttributeName= "status",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "nextExecution",
                            KeyType = "RANGE" //Sort key
                        }
                    }
                }
            };

            var createRequest = new CreateTableRequest($"{_tablePrefix}-{DynamoPersistenceProvider.WORKFLOW_TABLE}", new List<KeySchemaElement>()
            {
                new KeySchemaElement("id", KeyType.HASH)
            })
            {
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition("id", ScalarAttributeType.S),
                    new AttributeDefinition("status", ScalarAttributeType.S),
                    new AttributeDefinition("nextExecution", ScalarAttributeType.N),
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>()
                {
                    runnableIndex
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            await CreateTable(createRequest);
        }

        private async Task EnsureTable(string tableName, Action createTask)
        {
            try
            {
                var poll = await _client.DescribeTableAsync(tableName);
            }
            catch (ResourceNotFoundException)
            {
                createTask();
            }
        }

        private async Task CreateTable(CreateTableRequest createRequest)
        {
            var createResponse = await _client.CreateTableAsync(createRequest);

            int i = 0;
            bool created = false;
            while ((i < 10) && (!created))
            {
                try
                {
                    await Task.Delay(1000);
                    var poll = await _client.DescribeTableAsync(createRequest.TableName);
                    created = (poll.Table.TableStatus == TableStatus.ACTIVE);
                    i++;
                }
                catch (ResourceNotFoundException)
                {
                }
            }
        }
    }
}

