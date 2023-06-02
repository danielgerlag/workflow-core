using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using WorkflowCore.Providers.AWS.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class KinesisTracker : IKinesisTracker
    {
        private readonly ILogger _logger;
        private readonly AmazonDynamoDBClient _client;
        private readonly string _tableName;
        private bool _tableConfirmed = false;

        public KinesisTracker(AmazonDynamoDBClient client, string tableName, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger(GetType());
            _client = client;
            _tableName = tableName;
        }

        public async Task<string> GetNextShardIterator(string app, string stream, string shard)
        {
            if (!_tableConfirmed)
                await EnsureTable();

            var response = await _client.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(FormatId(app, stream, shard)) }
                }
            });

            if (!response.Item.ContainsKey("next_iterator"))
                return null;

            return response.Item["next_iterator"].S;
        }

        public async Task<string> GetNextLastSequenceNumber(string app, string stream, string shard)
        {
            if (!_tableConfirmed)
                await EnsureTable();

            var response = await _client.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue(FormatId(app, stream, shard)) }
                }
            });

            if (!response.Item.ContainsKey("last_sequence"))
                return null;

            return response.Item["last_sequence"].S;
        }

        public async Task IncrementShardIterator(string app, string stream, string shard, string iterator)
        {
            if (!_tableConfirmed)
                await EnsureTable();

            await _client.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"id", new AttributeValue(FormatId(app, stream, shard))}
                },
                UpdateExpression = "SET next_iterator = :n",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":n" , new AttributeValue(iterator) }
                }
            });
        }

        public async Task IncrementShardIteratorAndSequence(string app, string stream, string shard, string iterator, string sequence)
        {
            if (!_tableConfirmed)
                await EnsureTable();

            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    {"id", new AttributeValue(FormatId(app, stream, shard))},
                    {"next_iterator", new AttributeValue(iterator)},
                    {"last_sequence", new AttributeValue(sequence)}
                }
            });
        }

        private async Task EnsureTable()
        {
            try
            {
                var poll = await _client.DescribeTableAsync(_tableName);
                _tableConfirmed = true;
            }
            catch (ResourceNotFoundException)
            {
                await CreateTable();
            }
        }

        private async Task CreateTable()
        {
            var createRequest = new CreateTableRequest(_tableName, new List<KeySchemaElement>
            {
                new KeySchemaElement("id", KeyType.HASH)
            })
            {
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("id", ScalarAttributeType.S)
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            await _client.CreateTableAsync(createRequest);

            int i = 0;
            while (i < 20)
            {
                try
                {
                    i++;
                    await Task.Delay(1000);
                    var poll = await _client.DescribeTableAsync(_tableName);
                    if (poll.Table.TableStatus == TableStatus.ACTIVE)
                    {
                        _tableConfirmed = true;
                        return;
                    }
                }
                catch (ResourceNotFoundException)
                {
                }
            }
        }

        private string FormatId(string app, string stream, string shard) => $"{app}.{stream}.{shard}";
    }
}
