using System;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowCore.Persistence.SqlServer
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<SqlServerPersistenceProvider>
    {
        public SqlServerPersistenceProvider CreateDbContext(string[] args)
        {
            return new SqlServerPersistenceProvider(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;", true, true);
        }
    }
}
