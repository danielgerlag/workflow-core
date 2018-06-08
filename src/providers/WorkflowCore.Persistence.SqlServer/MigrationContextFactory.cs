using System;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowCore.Persistence.SqlServer
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<SqlServerContext>
    {
        public SqlServerContext CreateDbContext(string[] args)
        {
            return new SqlServerContext(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;");
        }
    }
}
