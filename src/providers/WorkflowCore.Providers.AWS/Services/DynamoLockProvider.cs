using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class DynamoLockProvider : IDistributedLockProvider
    {
        private readonly ILogger _logger;
        private readonly IAmazonDynamoDB _client;
        private readonly string _tableName;
        private readonly string _nodeId;    
        private readonly long _ttl = 30000;
        private readonly int _heartbeat = 10000;
        private readonly long _jitter = 1000;
        private readonly List<string> _localLocks;
        private Task _heartbeatTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);
        private readonly IDateTimeProvider _dateTimeProvider;

        public DynamoLockProvider(AWSCredentials credentials, AmazonDynamoDBConfig config, string tableName, ILoggerFactory logFactory, IDateTimeProvider dateTimeProvider)
        {
            _logger = logFactory.CreateLogger<DynamoLockProvider>();
            _client = new AmazonDynamoDBClient(credentials, config);
            _localLocks = new List<string>();
            _tableName = tableName;
            _nodeId = Guid.NewGuid().ToString();
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            try
            {
                var req = new PutItemRequest()
                {
                    TableName = _tableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue(Id) },
                        { "lock_owner", new AttributeValue(_nodeId) },
                        { "expires", new AttributeValue()
                            {
                                N = Convert.ToString(new DateTimeOffset(_dateTimeProvider.UtcNow).ToUnixTimeMilliseconds() + _ttl)
                            }
                        }
                    },
                    ConditionExpression = "attribute_not_exists(id) OR (expires < :expired)",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":expired", new AttributeValue()
                            {
                                N = Convert.ToString(new DateTimeOffset(_dateTimeProvider.UtcNow).ToUnixTimeMilliseconds() + _jitter)
                            }
                        }
                    }
                };

                var response = await _client.PutItemAsync(req, _cancellationTokenSource.Token);                                

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _localLocks.Add(Id);
                    return true;
                }
            }
            catch (ConditionalCheckFailedException)
            {
            }
            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            _mutex.WaitOne();
            try
            {
                _localLocks.Remove(Id);
            }
            finally
            {
                _mutex.Set();
            }
            
            try
            {
                var req = new DeleteItemRequest()
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue(Id) }
                    },
                    ConditionExpression = "lock_owner = :nodeId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":nodeId", new AttributeValue(_nodeId) }
                    }

                };
                await _client.DeleteItemAsync(req);
            }
            catch (ConditionalCheckFailedException)
            {     
            }
        }

        public async Task Start()
        {
            await EnsureTable();
            if (_heartbeatTask != null)
            {
                throw new InvalidOperationException();
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _heartbeatTask = new Task(SendHeartbeat);
            _heartbeatTask.Start();
        }

        public Task Stop()
        {
            _cancellationTokenSource.Cancel();
            _heartbeatTask.Wait();
            _heartbeatTask = null;
            return Task.CompletedTask;
        }

        private async void SendHeartbeat()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_heartbeat, _cancellationTokenSource.Token);
                    if (_mutex.WaitOne())
                    {
                        try
                        {
                            foreach (var item in _localLocks.ToArray())
                            {
                                var req = new PutItemRequest
                                {
                                    TableName = _tableName,
                                    Item = new Dictionary<string, AttributeValue>
                                    {
                                        { "id", new AttributeValue(item) },
                                        { "lock_owner", new AttributeValue(_nodeId) },
                                        { "expires", new AttributeValue()
                                            {
                                                N = Convert.ToString(new DateTimeOffset(_dateTimeProvider.UtcNow).ToUnixTimeMilliseconds() + _ttl)
                                            }
                                        }
                                    },
                                    ConditionExpression = "lock_owner = :nodeId",
                                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                                    {
                                        { ":nodeId", new AttributeValue(_nodeId) }
                                    }
                                };

                                try
                                {
                                    await _client.PutItemAsync(req, _cancellationTokenSource.Token);
                                }
                                catch (ConditionalCheckFailedException)
                                {
                                    _logger.LogWarning($"Lock not owned anymore when sending heartbeat for {item}");
                                }
                            }
                        }
                        finally
                        {
                            _mutex.Set();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(default(EventId), ex, ex.Message);
                }
            }
        }

        private async Task EnsureTable()
        {
            try
            {
                var poll = await _client.DescribeTableAsync(_tableName);
            }
            catch (ResourceNotFoundException)
            {
                await CreateTable();
            }
        }

        private async Task CreateTable()
        {
            var createRequest = new CreateTableRequest(_tableName, new List<KeySchemaElement>()
            {
                new KeySchemaElement("id", KeyType.HASH)
            })
            {
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition("id", ScalarAttributeType.S)
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            var createResponse = await _client.CreateTableAsync(createRequest);

            int i = 0;
            bool created = false;
            while ((i < 20) && (!created))
            {
                try
                {
                    await Task.Delay(1000);
                    var poll = await _client.DescribeTableAsync(_tableName);
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