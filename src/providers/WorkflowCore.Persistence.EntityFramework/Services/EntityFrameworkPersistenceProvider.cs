using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public abstract class EntityFrameworkPersistenceProvider : DbContext, IPersistenceProvider
    {
        protected readonly bool _canCreateDB;
        protected readonly bool _canMigrateDB;
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);

        protected EntityFrameworkPersistenceProvider(bool canCreateDB, bool canMigrateDB)
        {
            _canCreateDB = canCreateDB;
            _canMigrateDB = canMigrateDB;            
        }

        protected abstract void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder);
        protected abstract void ConfigureExecutionPointerStorage(EntityTypeBuilder<PersistedExecutionPointer> builder);
        protected abstract void ConfigureExecutionErrorStorage(EntityTypeBuilder<PersistedExecutionError> builder);
        protected abstract void ConfigureExetensionAttributeStorage(EntityTypeBuilder<PersistedExtensionAttribute> builder);
        protected abstract void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder);
        protected abstract void ConfigureEventStorage(EntityTypeBuilder<PersistedEvent> builder);
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var workflows = modelBuilder.Entity<PersistedWorkflow>();
            workflows.HasIndex(x => x.InstanceId).IsUnique();
            workflows.HasIndex(x => x.NextExecution);

            var executionPointers = modelBuilder.Entity<PersistedExecutionPointer>();
            var executionErrors = modelBuilder.Entity<PersistedExecutionError>();
            var extensionAttributes = modelBuilder.Entity<PersistedExtensionAttribute>();

            var subscriptions = modelBuilder.Entity<PersistedSubscription>();
            subscriptions.HasIndex(x => x.SubscriptionId).IsUnique();
            subscriptions.HasIndex(x => x.EventName);
            subscriptions.HasIndex(x => x.EventKey);

            var events = modelBuilder.Entity<PersistedEvent>();
            events.HasIndex(x => x.EventId).IsUnique();
            events.HasIndex(x => new { x.EventName, x.EventKey });            
            events.HasIndex(x => x.EventTime);
            events.HasIndex(x => x.IsProcessed);

            ConfigureWorkflowStorage(workflows);
            ConfigureExecutionPointerStorage(executionPointers);
            ConfigureExecutionErrorStorage(executionErrors);
            ConfigureExetensionAttributeStorage(extensionAttributes);
            ConfigureSubscriptionStorage(subscriptions);
            ConfigureEventStorage(events);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }        

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            _mutex.WaitOne();
            try
            {
                subscription.Id = Guid.NewGuid().ToString();
                var persistable = subscription.ToPersistable();
                var result = Set<PersistedSubscription>().Add(persistable);
                await SaveChangesAsync();
                Entry(persistable).State = EntityState.Detached;
                return subscription.Id;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            _mutex.WaitOne();
            try
            {
                workflow.Id = Guid.NewGuid().ToString();
                var persistable = workflow.ToPersistable();
                var result = Set<PersistedWorkflow>().Add(persistable);
                await SaveChangesAsync();
                Entry(persistable).State = EntityState.Detached;
                return workflow.Id;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            _mutex.WaitOne();
            try
            {
                var now = asAt.ToUniversalTime().Ticks;
                var raw = await Set<PersistedWorkflow>()
                    .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                    .Select(x => x.InstanceId)
                    .ToListAsync();

                return raw.Select(s => s.ToString()).ToList();
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            _mutex.WaitOne();
            try
            {
                IQueryable<PersistedWorkflow> query = Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);

                if (!String.IsNullOrEmpty(type))
                    query = query.Where(x => x.WorkflowDefinitionId == type);

                if (createdFrom.HasValue)
                    query = query.Where(x => x.CreateTime >= createdFrom.Value);

                if (createdTo.HasValue)
                    query = query.Where(x => x.CreateTime <= createdTo.Value);

                var rawResult = await query.Skip(skip).Take(take).ToListAsync();
                List<WorkflowInstance> result = new List<WorkflowInstance>();

                foreach (var item in rawResult)
                    result.Add(item.ToWorkflowInstance());

                return result;
            }
            finally
            {
                _mutex.Set();
            }
        }
        
        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            _mutex.WaitOne();
            try
            {
                var uid = new Guid(Id);
                var raw = await Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .FirstAsync(x => x.InstanceId == uid);

                if (raw == null)
                    return null;

                return raw.ToWorkflowInstance();
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            _mutex.WaitOne();
            try
            {
                var uid = new Guid(workflow.Id);
                var existingEntity = await Set<PersistedWorkflow>()
                    .Where(x => x.InstanceId == uid)
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsTracking()
                    .FirstAsync();

                var persistable = workflow.ToPersistable(existingEntity);
                await SaveChangesAsync();
                Entry(persistable).State = EntityState.Detached;
                foreach (var ep in persistable.ExecutionPointers)
                {
                    Entry(ep).State = EntityState.Detached;

                    foreach (var attr in ep.ExtensionAttributes)
                        Entry(attr).State = EntityState.Detached;

                }
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            _mutex.WaitOne();
            try
            {
                var uid = new Guid(eventSubscriptionId);
                var existing = await Set<PersistedSubscription>().FirstAsync(x => x.SubscriptionId == uid);
                Set<PersistedSubscription>().Remove(existing);
                await SaveChangesAsync();
            }
            finally
            {
                _mutex.Set();
            }
        }
                
        public virtual void EnsureStoreExists()
        {
            if (_canCreateDB && !_canMigrateDB)
            {
                Database.EnsureCreated();
                return;
            }

            if (_canMigrateDB)
            {
                Database.Migrate();
                return;
            }
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
        {
            _mutex.WaitOne();
            try
            {
                var raw = await Set<PersistedSubscription>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                    .ToListAsync();

                return raw.Select(item => item.ToEventSubscription()).ToList();
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            _mutex.WaitOne();
            try
            {
                newEvent.Id = Guid.NewGuid().ToString();
                var persistable = newEvent.ToPersistable();
                var result = Set<PersistedEvent>().Add(persistable);
                await SaveChangesAsync();
                Entry(persistable).State = EntityState.Detached;
                return newEvent.Id;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<Event> GetEvent(string id)
        {
            _mutex.WaitOne();
            try
            {
                Guid uid = new Guid(id);
                var raw = await Set<PersistedEvent>()
                    .FirstAsync(x => x.EventId == uid);

                if (raw == null)
                    return null;

                return raw.ToEvent();
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var now = asAt.ToUniversalTime();
            _mutex.WaitOne();
            try
            {
                var raw = await Set<PersistedEvent>()
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= now)
                    .Select(x => x.EventId)
                    .ToListAsync();

                return raw.Select(s => s.ToString()).ToList();
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task MarkEventProcessed(string id)
        {
            _mutex.WaitOne();
            try
            {
                var uid = new Guid(id);
                var existingEntity = await Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync();

                existingEntity.IsProcessed = true;
                await SaveChangesAsync();
                Entry(existingEntity).State = EntityState.Detached;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            _mutex.WaitOne();
            try
            {
                var raw = await Set<PersistedEvent>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .Select(x => x.EventId)
                    .ToListAsync();

                var result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task MarkEventUnprocessed(string id)
        {
            _mutex.WaitOne();
            try
            {
                var uid = new Guid(id);
                var existingEntity = await Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync();

                existingEntity.IsProcessed = false;
                await SaveChangesAsync();
                Entry(existingEntity).State = EntityState.Detached;
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            _mutex.WaitOne();
            try
            {
                var executionErrors = errors as ExecutionError[] ?? errors.ToArray();
                if (executionErrors.Any())
                {
                    foreach (var error in executionErrors)
                    {
                        Set<PersistedExecutionError>().Add(error.ToPersistable());
                    }
                    await SaveChangesAsync();

                }
            }
            finally
            {
                _mutex.Set();
            }
        }
    }
}
