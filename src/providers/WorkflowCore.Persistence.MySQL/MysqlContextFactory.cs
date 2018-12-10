using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.MySQL
{
    public class MysqlContextFactory : IWorkflowDbContextFactory
    {
        private readonly string _connectionString;
        private readonly Action<MySqlDbContextOptionsBuilder> _mysqlOptionsAction;

        public MysqlContextFactory(string connectionString, Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            _connectionString = connectionString;
            _mysqlOptionsAction = mysqlOptionsAction;
        }

        public WorkflowDbContext Build()
        {
            return new MysqlContext(_connectionString, _mysqlOptionsAction);
        }
    }
}
