using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
using System.Threading;

namespace WorkflowCore.Persistence.RavenDB.Services
{
	public class WorkflowPurger : IWorkflowPurger
	{
		private readonly IDocumentStore _database;

		public WorkflowPurger(IDocumentStore database)
		{
			_database = database;
		}

		public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default)
		{
			await DeleteWorkflowInstances(status, olderThan, cancellationToken);
		}


		/// <summary>
		/// Delete all Workflow Documents
		/// </summary>
		/// <returns></returns>
		private Task<Operation> DeleteWorkflowInstances(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default)
		{
			var utcTime = olderThan.ToUniversalTime();
			var queryToDelete = new IndexQuery { Query = $"FROM {nameof(WorkflowInstance)} where status = '{status}' and CompleteTime < '{olderThan}'" };
			return _database.Operations.SendAsync(new DeleteByQueryOperation(queryToDelete, new QueryOperationOptions { AllowStale = false }), token: cancellationToken);
		}
	}
}
