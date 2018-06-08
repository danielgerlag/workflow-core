using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.PostgreSQL
{
    public class PostgresContextFactory : IWorkflowDbContextFactory
    {
        private readonly string _connectionString;

        public PostgresContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public WorkflowDbContext Build()
        {
            return new PostgresContext(_connectionString);
        }
    }
}
