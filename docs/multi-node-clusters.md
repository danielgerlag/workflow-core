# Multi-node clusters

By default, the WorkflowHost service will run as a single node using the built-in queue and locking providers for a single node configuration.  Should you wish to run a multi-node cluster, you will need to configure an external queueing mechanism and a distributed lock manager to co-ordinate the cluster.  These are the providers that are currently available.

## Queue Providers

* SingleNodeQueueProvider *(Default built-in provider)*
* [Azure Storage Queues](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Redis](redis.md)
* [RabbitMQ](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.QueueProviders.RabbitMQ)
* [AWS Simple Queue Service](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)

`UseRedisQueues(connection, prefix)` defaults to `RedisQueueStorage.List`. Opt in to a sorted set with `RedisQueueStorage.SortedSet`. Do not mix modes on the same prefix — see [Redis queue storage](redis.md#queue-storage-list-vs-sortedset).

## Distributed lock managers

* SingleNodeLockProvider *(Default built-in provider)*
* [Azure Storage Leases](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Redis](redis.md)
* [AWS DynamoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)

