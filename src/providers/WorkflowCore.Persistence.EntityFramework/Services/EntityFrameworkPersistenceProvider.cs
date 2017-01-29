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
        protected abstract void ConfigurePublicationStorage(EntityTypeBuilder<PersistedPublication> builder);
        
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

            var publications = modelBuilder.Entity<PersistedPublication>();
            publications.HasIndex(x => x.PublicationId).IsUnique();

            ConfigureWorkflowStorage(workflows);
            ConfigureExecutionPointerStorage(executionPointers);
            ConfigureExecutionErrorStorage(executionErrors);
            ConfigureExetensionAttributeStorage(extensionAttributes);
            ConfigureSubscriptionStorage(subscriptions);
            ConfigurePublicationStorage(publications);
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
                        .ThenInclude(ep => ep.Errors)
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

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey)
        {
            lock (this)
            {
                var raw = Set<PersistedSubscription>().Where(x => x.EventName == eventName && x.EventKey == eventKey).ToList();

                List<EventSubscription> result = new List<EventSubscription>();
                foreach (var item in raw)
                    result.Add(item.ToEventSubscription());

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
                        .ThenInclude(ep => ep.Errors)
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
                        .ThenInclude(ep => ep.Errors)
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

                    foreach (var err in ep.Errors)
                        Entry(err).State = EntityState.Detached;
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

        public async Task CreateUnpublishedEvent(EventPublication publication)
        {
            lock (this)
            {
                var persistable = publication.ToPersistable();
                var result = Set<PersistedPublication>().Add(persistable);
                SaveChanges();
                Entry(persistable).State = EntityState.Detached;
            }
        }

        public async Task<IEnumerable<EventPublication>> GetUnpublishedEvents()
        {
            lock (this)
            {
                var raw = Set<PersistedPublication>().ToList();

                List<EventPublication> result = new List<EventPublication>();
                foreach (var item in raw)
                    result.Add(item.ToEventPublication());

                return result;
            }
        }

        public async Task RemoveUnpublishedEvent(Guid id)
        {
            lock (this)
            {
                var existing = Set<PersistedPublication>().First(x => x.PublicationId == id);
                Set<PersistedPublication>().Remove(existing);
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
                
    }
}
