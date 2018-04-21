#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer
{
    public class SqlServerQueueProviderOption
    {
        public SqlServerQueueProviderOption()
        {
            WorkflowHostName = "default";
        }

        public string ConnectionString { get; set; }
        public string WorkflowHostName { get; set; }
        public bool CanMigrateDb { get; set; }
        public bool CanCreateDb { get; set; }
    }
}