using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Providers.AWS.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class DynamoDbProvisioner : IDynamoDbProvisioner
    {
        private readonly ILogger _logger;
        private readonly IAmazonDynamoDB _client;
        private readonly string _tablePrefix;

        public DynamoDbProvisioner(AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<DynamoDbProvisioner>();
            _client = new AmazonDynamoDBClient(credentials, config);
            _tablePrefix = tablePrefix;
        }

        public Task ProvisionTables()
        {
            return Task.WhenAll(
                EnsureTable($"{_tablePrefix}-{DynamoPersistenceProvider.WORKFLOW_TABLE}", CreateWorkflowTable),
                EnsureTable($"{_tablePrefix}-{DynamoPersistenceProvider.SUBCRIPTION_TABLE}", CreateSubscriptionTable),
                EnsureTable($"{_tablePrefix}-{DynamoPersistenceProvider.EVENT_TABLE}", CreateEventTable));
        }

        private async Task CreateWorkflowTable()
        {
            var runnableIndex = new GlobalSecondaryIndex()
            {
                IndexName = "ix_runnable",
                KeySchema = new List<KeySchemaElement>()
                {
                    {
                        new KeySchemaElement
                        {
                            AttributeName= "runnable",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "next_execution",
                            KeyType = "RANGE" //Sort key
                        }
                    }
                },
                Projection = new Projection()
                {
                    ProjectionType = ProjectionType.KEYS_ONLY
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
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
                    new AttributeDefinition("runnable", ScalarAttributeType.N),
                    new AttributeDefinition("next_execution", ScalarAttributeType.N),
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>()
                {
                    runnableIndex
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            await CreateTable(createRequest);
        }

        private async Task CreateSubscriptionTable()
        {
            var slugIndex = new GlobalSecondaryIndex()
            {
                IndexName = "ix_slug",
                KeySchema = new List<KeySchemaElement>()
                {
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "event_slug",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "subscribe_as_of",
                            KeyType = "RANGE" //Sort key
                        }
                    }
                },
                Projection = new Projection()
                {
                    ProjectionType = ProjectionType.ALL
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };
            
            var createRequest = new CreateTableRequest($"{_tablePrefix}-{DynamoPersistenceProvider.SUBCRIPTION_TABLE}", new List<KeySchemaElement>()
            {
                new KeySchemaElement("id", KeyType.HASH)
            })
            {
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition("id", ScalarAttributeType.S),
                    new AttributeDefinition("event_slug", ScalarAttributeType.S),
                    new AttributeDefinition("subscribe_as_of", ScalarAttributeType.N)
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>()
                {
                    slugIndex
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            await CreateTable(createRequest);
        }

        private async Task CreateEventTable()
        {
            var slugIndex = new GlobalSecondaryIndex()
            {
                IndexName = "ix_slug",
                KeySchema = new List<KeySchemaElement>()
                {
                    {
                        new KeySchemaElement
                        {
                            AttributeName= "event_slug",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "event_time",
                            KeyType = "RANGE" //Sort key
                        }
                    }
                },
                Projection = new Projection()
                {
                    ProjectionType = ProjectionType.KEYS_ONLY
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            var processedIndex = new GlobalSecondaryIndex()
            {
                IndexName = "ix_not_processed",
                KeySchema = new List<KeySchemaElement>()
                {
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "not_processed",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "event_time",
                            KeyType = "RANGE" //Sort key
                        }
                    }
                },
                Projection = new Projection()
                {
                    ProjectionType = ProjectionType.KEYS_ONLY
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            var createRequest = new CreateTableRequest($"{_tablePrefix}-{DynamoPersistenceProvider.EVENT_TABLE}", new List<KeySchemaElement>()
            {
                new KeySchemaElement("id", KeyType.HASH)
            })
            {
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition("id", ScalarAttributeType.S),
                    new AttributeDefinition("not_processed", ScalarAttributeType.N),
                    new AttributeDefinition("event_slug", ScalarAttributeType.S),
                    new AttributeDefinition("event_time", ScalarAttributeType.N)
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>()
                {
                    slugIndex,
                    processedIndex
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            await CreateTable(createRequest);
        }

        private async Task EnsureTable(string tableName, Func<Task> createTask)
        {
            try
            {
                await _client.DescribeTableAsync(tableName);
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning($"Provisioning DynamoDb table - {tableName}");
                await createTask();
            }
        }

        private async Task CreateTable(CreateTableRequest createRequest)
        {
            var createResponse = await _client.CreateTableAsync(createRequest);

            int i = 0;
            bool created = false;
            while ((i < 30) && (!created))
            {
                try
                {
                    await Task.Delay(2000);
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

