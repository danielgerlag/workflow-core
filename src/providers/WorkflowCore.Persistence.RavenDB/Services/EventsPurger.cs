using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.RavenDB.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly IDocumentStore _database;

        public EventsPurger(IDocumentStore database)
        {
            _database = database;
        }

        public EventsPurgerOptions Options => throw new NotImplementedException();

        public Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var utcTime = olderThan.ToUniversalTime();
            var queryToDelete = new IndexQuery { Query = $"FROM {nameof(Event)} where EventTime < = '{olderThan}' and IsProcessed = '{true}'" };
            return _database.Operations.SendAsync(new DeleteByQueryOperation(queryToDelete, new QueryOperationOptions { AllowStale = false }), token: cancellationToken);
        }
    }
}
