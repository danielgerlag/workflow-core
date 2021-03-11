using System;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.Sqlite
{
    public class SqliteContextFactory : IWorkflowDbContextFactory
    {
        private readonly string _connectionString;

        public SqliteContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public WorkflowDbContext Build()
        {
            return new SqliteContext(_connectionString);
        }
    }
}
