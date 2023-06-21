using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Models;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly IWorkflowDbContextFactory _contextFactory;

        public EventsPurger(IWorkflowDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Event table purger
        /// </summary>
        /// <param name="olderThan"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            using (var db = ConstructDbContext())
            {
                var events = db.Set<PersistedEvent>()
                    .Where(x => x.EventTime < olderThanUtc &&
                                x.IsProcessed == true);

                db.RemoveRange(events);
                await db.SaveChangesAsync(cancellationToken);
            }
        }


        private WorkflowDbContext ConstructDbContext()
        {
            return _contextFactory.Build();
        }
    }
}
