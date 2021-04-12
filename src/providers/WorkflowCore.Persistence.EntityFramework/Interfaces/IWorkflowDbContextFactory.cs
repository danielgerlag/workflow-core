using System;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.EntityFramework.Interfaces
{
    public interface IWorkflowDbContextFactory
    {
        WorkflowDbContext Build();
    }
}
