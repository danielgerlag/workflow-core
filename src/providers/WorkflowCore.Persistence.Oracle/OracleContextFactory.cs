using System;

using Oracle.EntityFrameworkCore.Infrastructure;

using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.Oracle
{
    public class OracleContextFactory : IWorkflowDbContextFactory
    {
        private readonly string _connectionString;
        private readonly Action<OracleDbContextOptionsBuilder> _oracleOptionsAction;

        public OracleContextFactory(string connectionString, Action<OracleDbContextOptionsBuilder> oracleOptionsAction = null)
        {
            _connectionString = connectionString;
            _oracleOptionsAction = oracleOptionsAction;
        }

        public WorkflowDbContext Build()
        {
            return new OracleContext(_connectionString, _oracleOptionsAction);
        }
    }
}
