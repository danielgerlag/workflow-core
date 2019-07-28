# Multi-node clusters

By default, the WorkflowHost service will run as a single node using the built-in queue and locking providers for a single node configuration.  Should you wish to run a multi-node cluster, you will need to configure an external queueing mechanism and a distributed lock manager to co-ordinate the cluster.  These are the providers that are currently available.

## Queue Providers

* SingleNodeQueueProvider *(Default built-in provider)*
* [Azure Storage Queues](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Redis](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Redis)
* [RabbitMQ](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.QueueProviders.RabbitMQ)
* [AWS Simple Queue Service](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)


## Distributed lock managers

* SingleNodeLockProvider *(Default built-in provider)*
* [Azure Storage Leases](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Redis](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Redis)
* [AWS DynamoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)

