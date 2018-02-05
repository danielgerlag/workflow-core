#region using

using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

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
        private static ServiceProvider _serviceProvider;

        public SqlServerQueueProviderFixture(ITestOutputHelper output, SqlDockerSetup setup)
        {
            Console = output;
            var connectionString = SqlDockerSetup.ConnectionString;
            ConfigureServices();

            var opt = new SqlServerQueueProviderOption(connectionString, "UnitTest", true, true);
            QueueProvider = new SqlServerQueueProvider(_serviceProvider, opt);
            QueueProvider.Start().Wait();

            Setup();
        }

        private static void ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            QueueProvider.Dispose();
        }
    }
}