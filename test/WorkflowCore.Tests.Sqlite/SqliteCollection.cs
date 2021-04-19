using System;
using Xunit;

namespace WorkflowCore.Tests.Sqlite
{
    [CollectionDefinition("Sqlite collection")]
    public class SqliteCollection : ICollectionFixture<SqliteSetup>
    {        
    }

    public class SqliteSetup : IDisposable
    {
        public string ConnectionString { get; set; }

        public SqliteSetup()
        {
            ConnectionString = $"Data Source=wfc-tests-{DateTime.Now.Ticks}.db;";
        }

        public void Dispose()
        {
            
        }
    }
}
