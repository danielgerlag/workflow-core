#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProviderOption
    {
        public SqlServerQueueProviderOption(string connectionString, string workflowHostName, bool canMigrateDb, bool canCreateDb)
        {
            ConnectionString = connectionString;
            WorkflowHostName = workflowHostName;
            CanMigrateDb = canMigrateDb;
            CanCreateDb = canCreateDb;
        }

        public string ConnectionString { get; private set; }
        public string WorkflowHostName { get; private set; }
        public bool CanMigrateDb { get; private set; }
        public bool CanCreateDb { get; private set; }
    }
}