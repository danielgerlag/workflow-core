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
        protected abstract void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder);
        protected abstract void ConfigurePublicationStorage(EntityTypeBuilder<PersistedPublication> builder);
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var workflows = modelBuilder.Entity<PersistedWorkflow>();
            workflows.HasIndex(x => x.InstanceId).IsUnique();
            workflows.HasIndex(x => x.NextExecution);            

            var subscriptions = modelBuilder.Entity<PersistedSubscription>();
            subscriptions.HasIndex(x => x.SubscriptionId).IsUnique();
            subscriptions.HasIndex(x => x.EventName);
            subscriptions.HasIndex(x => x.EventKey);

            var publications = modelBuilder.Entity<PersistedPublication>();
            publications.HasIndex(x => x.PublicationId).IsUnique();

            ConfigureWorkflowStorage(workflows);
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
            subscription.Id = Guid.NewGuid().ToString();
            var persistable = subscription.ToPersistable();
            var result = Set<PersistedSubscription>().Add(persistable);
            SaveChanges();
            Entry(persistable).State = EntityState.Detached;
            return subscription.Id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();
            var persistable = workflow.ToPersistable();
            var result = Set<PersistedWorkflow>().Add(persistable);
            SaveChanges();
            Entry(persistable).State = EntityState.Detached;
            return workflow.Id;
        }

        public async Task<IEnumerable<string>> GetRunnableInstances()
        {
            var now = DateTime.Now.ToUniversalTime().Ticks;
            var raw = Set<PersistedWorkflow>()
                .Where(x => x.NextExecution.HasValue && x.NextExecution <= now)
                .Select(x => x.InstanceId)
                .ToList();

            List<string> result = new List<string>();

            foreach (var s in raw)
                result.Add(s.ToString());

            return result;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey)
        {
            var raw = Set<PersistedSubscription>().Where(x => x.EventName == eventName && x.EventKey == eventKey).ToList();

            List<EventSubscription> result = new List<EventSubscription>();
            foreach (var item in raw)
                result.Add(item.ToEventSubscription());

            return result;
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            Guid uid = new Guid(Id);
            var raw = Set<PersistedWorkflow>().First(x => x.InstanceId == uid);

            if (raw == null)
                return null;

            return raw.ToWorkflowInstance();
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            Guid uid = new Guid(workflow.Id);            
            var existingKey = Set<PersistedWorkflow>().Where(x => x.InstanceId == uid).Select(x => x.ClusterKey).First();
            var persistable = workflow.ToPersistable();
            persistable.ClusterKey = existingKey;            
            Set<PersistedWorkflow>().Update(persistable);
            SaveChanges();
            Entry(persistable).State = EntityState.Detached;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            Guid uid = new Guid(eventSubscriptionId);
            var existing = Set<PersistedSubscription>().First(x => x.SubscriptionId == uid);
            Set<PersistedSubscription>().Remove(existing);
            SaveChanges();
        }

        public async Task CreateUnpublishedEvent(EventPublication publication)
        {
            var persistable = publication.ToPersistable();
            var result = Set<PersistedPublication>().Add(persistable);
            SaveChanges();
            Entry(persistable).State = EntityState.Detached;
        }

        public async Task<IEnumerable<EventPublication>> GetUnpublishedEvents()
        {
            var raw = Set<PersistedPublication>().ToList();

            List<EventPublication> result = new List<EventPublication>();
            foreach (var item in raw)
                result.Add(item.ToEventPublication());

            return result;
        }

        public async Task RemoveUnpublishedEvent(Guid id)
        {           
            var existing = Set<PersistedPublication>().First(x => x.PublicationId == id);
            Set<PersistedPublication>().Remove(existing);
            SaveChanges();
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
