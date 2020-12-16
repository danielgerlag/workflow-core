using Raven.Client.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.RavenDB.Services
{
	// TODO:  Implement Map for result bind of Index
	public class WorkflowInstances_Id : AbstractIndexCreationTask<WorkflowInstance> { }
	public class EventSubscriptions_Id : AbstractIndexCreationTask<EventSubscription> { }
	public class Events_Id : AbstractIndexCreationTask<Event> { }
	public class ExecutionErrors_Id : AbstractIndexCreationTask<ExecutionError> { }
}
