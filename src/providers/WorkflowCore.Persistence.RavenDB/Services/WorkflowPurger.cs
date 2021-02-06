using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Raven.Client;
using System.Threading.Tasks;
using Raven.Client.Documents;
using System.Linq;
using Raven.Client.Extensions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;

namespace WorkflowCore.Persistence.RavenDB.Services
{
	public class WorkflowPurger : IWorkflowPurger
	{
		private readonly IDocumentStore _database;

		public WorkflowPurger(IDocumentStore database)
		{
			_database = database;
		}

		public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan)
		{
			await DeleteWorkflowInstances(status, olderThan);
		}


		/// <summary>
		/// Delete all Workflow Documents
		/// </summary>
		/// <returns></returns>
		private Task<Operation> DeleteWorkflowInstances(WorkflowStatus status, DateTime olderThan)
		{
			var utcTime = olderThan.ToUniversalTime();
			var queryToDelete = new IndexQuery { Query = $"FROM {nameof(WorkflowInstance)} where status = '{status}' and CompleteTime < '{olderThan}'" };
			return _database.Operations.SendAsync(new DeleteByQueryOperation(queryToDelete, new QueryOperationOptions { AllowStale = false }));
		}
	}
}
