using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using System.Threading;
using WorkflowCore.Interface;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public sealed class LargeDataOptimizedEntityFrameworkPersistenceProvider : EntityFrameworkPersistenceProvider, IPersistenceProvider
    {
        private readonly IWorkflowDbContextFactory _contextFactory;

        public LargeDataOptimizedEntityFrameworkPersistenceProvider(IWorkflowDbContextFactory contextFactory, bool canCreateDb, bool canMigrateDb)
            : base(contextFactory, canCreateDb, canMigrateDb)
        {
            _contextFactory = contextFactory;
        }

        /// <inheritdoc/>
        public new async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            using (var db = _contextFactory.Build())
            {
                IQueryable<PersistedWorkflow> query = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsSplitQuery()
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(x => x.Status == status.Value);
                }

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(x => x.WorkflowDefinitionId == type);
                }

                if (createdFrom.HasValue)
                {
                    query = query.Where(x => x.CreateTime >= createdFrom.Value);
                }

                if (createdTo.HasValue)
                {
                    query = query.Where(x => x.CreateTime <= createdTo.Value);
                }

                var rawResult = await query.OrderBy(x => x.PersistenceId).Skip(skip).Take(take).ToListAsync();

                var result = new List<WorkflowInstance>(rawResult.Count);

                foreach (var item in rawResult)
                {
                    result.Add(item.ToWorkflowInstance());
                }

                return result;
            }
        }

        /// <inheritdoc/>
        public new async Task<WorkflowInstance> GetWorkflowInstance(string id, CancellationToken cancellationToken = default)
        {
            using (var db = _contextFactory.Build())
            {
                var uid = new Guid(id);
                var raw = await db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsSplitQuery()
                    .FirstAsync(x => x.InstanceId == uid, cancellationToken);

                return raw?.ToWorkflowInstance();
            }
        }

        /// <inheritdoc/>
        public new async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
            {
                return Array.Empty<WorkflowInstance>();
            }

            using (var db = _contextFactory.Build())
            {
                var uids = ids.Select(i => new Guid(i));
                var raw = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsSplitQuery()
                    .Where(x => uids.Contains(x.InstanceId));

                var persistedWorkflows = await raw.ToListAsync(cancellationToken);

                return persistedWorkflows.Select(i => i.ToWorkflowInstance());
            }
        }

        /// <inheritdoc/>
        public new async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            using (var db = _contextFactory.Build())
            using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                var uid = new Guid(workflow.Id);
                var existingEntity = await db.Set<PersistedWorkflow>()
                    .Where(x => x.InstanceId == uid)
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsSplitQuery()
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                _ = workflow.ToPersistable(existingEntity);

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
        }
    }
}