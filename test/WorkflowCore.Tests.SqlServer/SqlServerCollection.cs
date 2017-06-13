using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    [CollectionDefinition("SqlServer collection")]
    public class SqlServerCollection : ICollectionFixture<SqlServerSetup>
    {
    }

    public class SqlServerSetup : IDisposable
    {
        public string ConnectionString { get; set; }

        public SqlServerSetup()
        {
            ConnectionString = @"Server=.\SQLEXPRESS;Database=WorkflowCore-test;Trusted_Connection=True;";
        }

        public void Dispose()
        {

        }
    }
}
