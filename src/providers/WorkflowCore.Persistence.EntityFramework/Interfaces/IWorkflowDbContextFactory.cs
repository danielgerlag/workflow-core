using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.EntityFramework.Interfaces
{
    public interface IWorkflowDbContextFactory
    {
        WorkflowDbContext Build();
    }
}
