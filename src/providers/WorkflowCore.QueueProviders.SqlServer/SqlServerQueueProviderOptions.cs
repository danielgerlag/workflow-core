#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer
{
    public class SqlServerQueueProviderOptions
    {
        public string ConnectionString { get; set; }        
        public bool CanMigrateDb { get; set; }
        public bool CanCreateDb { get; set; }
    }
}