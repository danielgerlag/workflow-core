#region using

using System;
using System.Linq;

using WorkflowCore.QueueProviders.SqlServer;
using WorkflowCore.QueueProviders.SqlServer.Services;
using WorkflowCore.UnitTests;

using Xunit;
using Xunit.Abstractions;

#endregion

namespace WorkflowCore.Tests.SqlServer
{
    [Collection("SqlServer collection")]
    public class SqlServerQueueProviderFixture : BaseQueueProviderFixture, IDisposable
    {
        public SqlServerQueueProviderFixture(ITestOutputHelper output, SqlDockerSetup setup)
        {
            Console = output;
            var connectionString = SqlDockerSetup.ConnectionString;
        
            var opt = new SqlServerQueueProviderOption {
                ConnectionString = connectionString,
                WorkflowHostName = "UnitTest",
                CanCreateDb = true,
                CanMigrateDb = true
            };
            var names = new BrokerNamesProvider(opt.WorkflowHostName);
            var sqlCommandExecutor = new SqlCommandExecutor();
            var migrator = new SqlServerQueueProviderMigrator(opt.ConnectionString, names, sqlCommandExecutor);

            QueueProvider = new SqlServerQueueProvider(opt,names,migrator,sqlCommandExecutor);
            QueueProvider.Start().Wait();

            Setup();
        }

        public void Dispose()
        {
            QueueProvider.Dispose();
        }
    }
}