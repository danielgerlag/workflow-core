using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Models;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public class WorkflowPurger : IWorkflowPurger
    {
        private readonly IWorkflowDbContextFactory _contextFactory;

        public WorkflowPurger(IWorkflowDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            using (var db = ConstructDbContext())
            {
                var workflows = await db.Set<PersistedWorkflow>().Where(x => x.Status == status && x.CompleteTime < olderThanUtc).ToListAsync();
                foreach (var wf in workflows)
                {
                    foreach (var pointer in wf.ExecutionPointers)
                    {
                        foreach (var extAttr in pointer.ExtensionAttributes)
                        {
                            db.Remove(extAttr);
                        }

                        db.Remove(pointer);
                    }
                    db.Remove(wf);
                }

                await db.SaveChangesAsync();
            }
        }
        
        
        private WorkflowDbContext ConstructDbContext()
        {
            return _contextFactory.Build();
        }
    }
}