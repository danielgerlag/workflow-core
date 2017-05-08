using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public abstract class EntityFrameworkPersistenceProvider : DbContext, IPersistenceProvider
    {
        protected readonly bool _canCreateDB;
        protected readonly bool _canMigrateDB;

        public EntityFrameworkPersistenceProvider(bool canCreateDB, bool canMigrateDB)
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
            lock (this)
            {
                subscription.Id = Guid.NewGuid().ToString();
                var persistable = subscription.ToPersistable();
                var result = Set<PersistedSubscription>().Add(persistable);
                SaveChanges();
                Entry(persistable).State = EntityState.Detached;
                return subscription.Id;
            }
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            lock (this)
            {
                workflow.Id = Guid.NewGuid().ToString();
                var persistable = workflow.ToPersistable();
                var result = Set<PersistedWorkflow>().Add(persistable);
                SaveChanges();
                Entry(persistable).State = EntityState.Detached;
                return workflow.Id;
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances()
        {
            lock (this)
            {
                var now = DateTime.Now.ToUniversalTime().Ticks;
                var raw = Set<PersistedWorkflow>()
                    .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                    .Select(x => x.InstanceId)
                    .ToList();

                List<string> result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            lock (this)
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

                var rawResult = query.Skip(skip).Take(take).ToList();
                List<WorkflowInstance> result = new List<WorkflowInstance>();

                foreach (var item in rawResult)
                    result.Add(item.ToWorkflowInstance());

                return result;
            }
        }
        
        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            lock (this)
            {
                Guid uid = new Guid(Id);
                var raw = Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                        .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .First(x => x.InstanceId == uid);

                if (raw == null)
                    return null;

                return raw.ToWorkflowInstance();
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            lock (this)
            {
                Guid uid = new Guid(workflow.Id);
                var existingEntity = Set<PersistedWorkflow>()
                    .Where(x => x.InstanceId == uid)
                    .Include(wf => wf.ExecutionPointers)
                        .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsTracking()
                    .First();

                var persistable = workflow.ToPersistable(existingEntity);
                SaveChanges();
                Entry(persistable).State = EntityState.Detached;
                foreach (var ep in persistable.ExecutionPointers)
                {
                    Entry(ep).State = EntityState.Detached;

                    foreach (var attr in ep.ExtensionAttributes)
                        Entry(attr).State = EntityState.Detached;
                    
                }
            }
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            lock (this)
            {
                Guid uid = new Guid(eventSubscriptionId);
                var existing = Set<PersistedSubscription>().First(x => x.SubscriptionId == uid);
                Set<PersistedSubscription>().Remove(existing);
                SaveChanges();
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
            lock (this)
            {
                var raw = Set<PersistedSubscription>().Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf).ToList();

                List<EventSubscription> result = new List<EventSubscription>();
                foreach (var item in raw)
                    result.Add(item.ToEventSubscription());

                return result;
            }
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            lock (this)
            {
                newEvent.Id = Guid.NewGuid().ToString();
                var persistable = newEvent.ToPersistable();
                var result = Set<PersistedEvent>().Add(persistable);
                SaveChanges();
                Entry(persistable).State = EntityState.Detached;
                return newEvent.Id;
            }
        }

        public async Task<Event> GetEvent(string id)
        {
            lock (this)
            {
                Guid uid = new Guid(id);
                var raw = Set<PersistedEvent>()                    
                    .First(x => x.EventId == uid);

                if (raw == null)
                    return null;

                return raw.ToEvent();
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents()
        {
            var now = DateTime.Now.ToUniversalTime();

            lock (this)
            {
                var raw = Set<PersistedEvent>()
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= now)
                    .Select(x => x.EventId)
                    .ToList();

                List<string> result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
        }

        public async Task MarkEventProcessed(string id)
        {
            lock (this)
            {
                Guid uid = new Guid(id);
                var existingEntity = Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .First();

                existingEntity.IsProcessed = true;
                SaveChanges();
                Entry(existingEntity).State = EntityState.Detached;                
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            lock (this)
            {
                var raw = Set<PersistedEvent>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .Select(x => x.EventId)
                    .ToList();

                List<string> result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
        }

        public async Task MarkEventUnprocessed(string id)
        {
            lock (this)
            {
                Guid uid = new Guid(id);
                var existingEntity = Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .First();

                existingEntity.IsProcessed = false;
                SaveChanges();
                Entry(existingEntity).State = EntityState.Detached;
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            foreach (var error in errors)
            {
                Set<PersistedExecutionError>().Add(error.ToPersistable());
            }
            await SaveChangesAsync();
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
