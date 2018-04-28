using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.QueueProviders.SqlServer.Interfaces
{
    public interface ISqlServerQueueProviderMigrator
    {
        void MigrateDb();
        void CreateDb();
    }
}
