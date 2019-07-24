using System;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowCore.Persistence.PostgreSQL
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<PostgresContext>
    {
        public PostgresContext CreateDbContext(string[] args)
        {
            return new PostgresContext(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;Password=password;","wfc");
        }
    }
}
