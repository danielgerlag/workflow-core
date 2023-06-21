using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Models;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly IWorkflowDbContextFactory _contextFactory;
        public EventsPurgerOptions Options { get; }

        public EventsPurger(IWorkflowDbContextFactory contextFactory, EventsPurgerOptions options)
        {
            _contextFactory = contextFactory;
            Options = options;
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
                int deleteEvents = Options.BatchSize;
                db.Database.SetCommandTimeout(Options.DeleteCommandTimeoutSeconds);

                #if NET6_0_OR_GREATER
                    while(deleteEvents != 0)
                    {
                        deleteEvents = await db.Set<PersistedEvent>()
                            .Where(x => x.EventTime < olderThanUtc &&
                                        x.IsProcessed == true)
                            .Take(Options.BatchSize)
                            .ExecuteDeleteAsync(cancellationToken);

                    }
                #else
                    while (deleteEvents != 0)
                    {
                        var events = db.Set<PersistedEvent>()
                            .Where(x => x.EventTime < olderThanUtc &&
                                        x.IsProcessed == true)
                            .Take(Options.BatchSize);
                    
                        deleteEvents = await events.CountAsync();
                    
                        if(deleteEvents != 0)
                        {
                            db.RemoveRange(events);
                            await db.SaveChangesAsync(cancellationToken);
                        }
                    }  
                #endif
            }
        }


        private WorkflowDbContext ConstructDbContext()
        {
            return _contextFactory.Build();
        }
    }
}
