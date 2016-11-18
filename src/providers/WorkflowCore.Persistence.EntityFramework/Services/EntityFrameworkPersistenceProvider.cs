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

        protected abstract void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder);
        protected abstract void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder);

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

            ConfigureWorkflowStorage(workflows);
            ConfigureSubscriptionStorage(subscriptions);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        public abstract void EnsureStoreExists();

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
        
        
    }
}
