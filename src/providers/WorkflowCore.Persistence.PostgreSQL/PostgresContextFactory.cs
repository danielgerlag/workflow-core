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
        private readonly string _schemaName;

        public PostgresContextFactory(string connectionString, string schemaName)
        {
            _connectionString = connectionString;
            _schemaName = schemaName;
        }

        public WorkflowDbContext Build()
        {
            return new PostgresContext(_connectionString,_schemaName);
        }
    }
}
