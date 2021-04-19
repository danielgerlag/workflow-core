using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.SqlServer
{
    public class SqlContextFactory : IWorkflowDbContextFactory
    {
        private readonly string _connectionString;
        private readonly Action<DbConnection> _initAction;

        public SqlContextFactory(string connectionString, Action<DbConnection> initAction = null)
        {
            _connectionString = connectionString;
            _initAction = initAction;
        }
        
        public WorkflowDbContext Build()
        {
            var result = new SqlServerContext(_connectionString);
            _initAction?.Invoke(result.Database.GetDbConnection());

            return result;
        }
    }
}
