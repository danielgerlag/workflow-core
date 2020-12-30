using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.RavenDB.Services
{
	public class RavendbPersistenceProvider : IPersistenceProvider
	{
		internal const string WorkflowCollectionName = "wfc.workflows";
		private readonly IDocumentStore _database;
		static bool indexesCreated = false;

		public RavendbPersistenceProvider(IDocumentStore database)
		{
			_database = database;
			CreateIndexes(this);
		}

		static void CreateIndexes(RavendbPersistenceProvider instance)
		{
			if (!indexesCreated)
			{
				/*
				// create the indexes here based on assemby of classes in the file 'RavenDbIndexes.cs'
				IndexCreation.CreateIndexes(typeof(WorkflowInstances_Id).Assembly, instance._database);
				IndexCreation.CreateIndexes(typeof(EventSubscriptions_Id).Assembly, instance._database);
				IndexCreation.CreateIndexes(typeof(Events_Id).Assembly, instance._database);
				IndexCreation.CreateIndexes(typeof(ExecutionErrors_Id).Assembly, instance._database);
				*/
				indexesCreated = true;
			}
		}

		public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
		{
			using (var session = _database.OpenAsyncSession())
			{
				await session.StoreAsync(workflow);
				var id = workflow.Id;
				await session.SaveChangesAsync();
				return id;
			}
		}

		public async Task PersistWorkflow(WorkflowInstance workflow)
		{
			using (var session = _database.OpenAsyncSession())
			{
				session.Advanced.Patch<WorkflowInstance, string>(workflow.Id, x => x.WorkflowDefinitionId, workflow.WorkflowDefinitionId);
				session.Advanced.Patch<WorkflowInstance, int>(workflow.Id, x => x.Version, workflow.Version);
				session.Advanced.Patch<WorkflowInstance, string>(workflow.Id, x => x.Description, workflow.Description);
				session.Advanced.Patch<WorkflowInstance, string>(workflow.Id, x => x.Reference, workflow.Reference);
				session.Advanced.Patch<WorkflowInstance, ExecutionPointerCollection>(workflow.Id, x => x.ExecutionPointers, workflow.ExecutionPointers);
				session.Advanced.Patch<WorkflowInstance, long?>(workflow.Id, x => x.NextExecution, workflow.NextExecution);
				session.Advanced.Patch<WorkflowInstance, WorkflowStatus>(workflow.Id, x => x.Status, workflow.Status);
				session.Advanced.Patch<WorkflowInstance, object>(workflow.Id, x => x.Data, workflow.Data);
				session.Advanced.Patch<WorkflowInstance, DateTime>(workflow.Id, x => x.CreateTime, workflow.CreateTime);
				session.Advanced.Patch<WorkflowInstance, DateTime?>(workflow.Id, x => x.CompleteTime, workflow.CompleteTime);

				await session.SaveChangesAsync();
			}
		}

		public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
		{
			var now = asAt.ToUniversalTime().Ticks;
			using (var session = _database.OpenAsyncSession())
			{
				var l = session.Query<WorkflowInstance>().Where(w => w.NextExecution.HasValue
					&& (w.NextExecution <= now)
					&& (w.Status == WorkflowStatus.Runnable)
				).Select(x => x.Id);

				return await l.ToListAsync();
			}
		}

		public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var result = await session.Query<WorkflowInstance>().FirstOrDefaultAsync(x => x.Id == Id);
				return result;
			}
		}

		public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
		{
			if (ids == null)
			{
				return new List<WorkflowInstance>();
			}

			using (var session = _database.OpenAsyncSession())
			{
				var list = session.Query<WorkflowInstance>().Where(x => x.Id.In(ids));
				return await list.ToListAsync<WorkflowInstance>();
			}
		}

		public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var result = session.Query<WorkflowInstance>();

				if (status.HasValue)
					result = result.Where(x => x.Status == status.Value);

				if (!String.IsNullOrEmpty(type))
					result = result.Where(x => x.WorkflowDefinitionId == type);

				if (createdFrom.HasValue)
					result = result.Where(x => x.CreateTime >= createdFrom.Value);

				if (createdTo.HasValue)
					result = result.Where(x => x.CreateTime <= createdTo.Value);

				return await result.Skip(skip).Take(take).ToListAsync();
			}
		}

		public async Task<string> CreateEventSubscription(EventSubscription subscription)
		{
			using (var session = _database.OpenAsyncSession())
			{
				await session.StoreAsync(subscription);
				var id = subscription.Id;
				await session.SaveChangesAsync();
				return id;
			}
		}

		public async Task TerminateSubscription(string eventSubscriptionId)
		{
			using (var session = _database.OpenAsyncSession())
			{
				session.Delete(eventSubscriptionId);
				await Task.CompletedTask;
			}
		}

		public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var result = session.Query<EventSubscription>().Where(x => x.Id == eventSubscriptionId);
				return await result.FirstOrDefaultAsync();
			}
		}

		public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var q = session.Query<EventSubscription>().Where(x =>
					x.EventName == eventKey
					&& x.EventKey == eventKey
					&& x.SubscribeAsOf <= asOf
					&& x.ExternalToken == null
				);

				return await q.FirstOrDefaultAsync();
			}
		}

		public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
		{
			try
			{
				// The query string
				var strbuilder = new StringBuilder();
				strbuilder.Append("from EventSubscriptions as e ");
				strbuilder.Append($"where e.Id = {eventSubscriptionId} and ExternalToken = null");
				strbuilder.Append("update");
				strbuilder.Append("{");
				strbuilder.Append($"e.ExternalToken = '{token}'");
				strbuilder.Append($"e.ExternalTokenExpiry = '{expiry}'");
				strbuilder.Append($"e.ExternalWorkerId = 'workerId'");
				strbuilder.Append("}");

				var operation = await _database.Operations.SendAsync(new PatchByQueryOperation(strbuilder.ToString()));
				operation.WaitForCompletion();
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}

		public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
		{
			try
			{
				// The query string
				var strbuilder = new StringBuilder();
				strbuilder.Append("from EventSubscriptions as e ");
				strbuilder.Append($"where e.Id = {eventSubscriptionId} and ExternalToken = '{token}'");
				strbuilder.Append("update");
				strbuilder.Append("{");
				strbuilder.Append($"e.ExternalToken = null");
				strbuilder.Append($"e.ExternalTokenExpiry = null");
				strbuilder.Append($"e.ExternalWorkerId = null");
				strbuilder.Append("}");

				var operation = await _database.Operations.SendAsync(new PatchByQueryOperation(strbuilder.ToString()));
				operation.WaitForCompletion();
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public void EnsureStoreExists() { }

		public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var q = session.Query<EventSubscription>().Where(x =>
					x.EventName == eventName
					&& x.EventKey == eventKey
					&& x.SubscribeAsOf <= asOf
				);

				return await q.ToListAsync();
			}
		}

		public async Task<string> CreateEvent(Event newEvent)
		{
			using (var session = _database.OpenAsyncSession())
			{
				await session.StoreAsync(newEvent);
				var id = newEvent.Id;
				await session.SaveChangesAsync();
				return id;
			}
		}

		public async Task<Event> GetEvent(string id)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var result = session.Query<Event>().Where(x => x.Id == id);
				return await result.FirstAsync();
			}
		}

		public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var now = asAt.ToUniversalTime();
				var result = session.Query<Event>()
									.Where(x => !x.IsProcessed && x.EventTime < now)
									.Select(x => x.Id);

				return await result.ToListAsync();
			}
		}

		public async Task MarkEventProcessed(string id)
		{
			using (var session = _database.OpenAsyncSession())
			{
				session.Advanced.Patch<Event, bool>(id, x => x.IsProcessed, true);

				await session.SaveChangesAsync();
			}
		}

		public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
		{
			using (var session = _database.OpenAsyncSession())
			{
				var q = session.Query<Event>()
								.Where(x =>
									x.EventName == eventName
									&& x.EventKey == eventKey
									&& x.EventTime >= asOf)
								.Select(x => x.Id);

				return await q.ToListAsync();
			}
		}

		public async Task MarkEventUnprocessed(string id)
		{
			using (var session = _database.OpenAsyncSession())
			{
				session.Advanced.Patch<Event, bool>(id, x => x.IsProcessed, false);

				await session.SaveChangesAsync();
			}
		}

		public async Task PersistErrors(IEnumerable<ExecutionError> errors)
		{
			if (errors.Any())
			{
				var blk = _database.BulkInsert();
				foreach (var error in errors)
					await blk.StoreAsync(error);

			}
		}

	}
}
