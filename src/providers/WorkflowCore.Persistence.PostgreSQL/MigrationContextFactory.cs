using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Persistence.PostgreSQL
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<PostgresPersistenceProvider>
    {
        public PostgresPersistenceProvider CreateDbContext(string[] args)
        {
            return new PostgresPersistenceProvider(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;Password=password;", true, true);
        }
    }
}
