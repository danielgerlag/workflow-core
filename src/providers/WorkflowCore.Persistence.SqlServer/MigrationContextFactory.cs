using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Persistence.SqlServer
{
    public class MigrationContextFactory : IDbContextFactory<SqlServerPersistenceProvider>
    {
        public SqlServerPersistenceProvider Create(DbContextFactoryOptions options)
        {
            return new SqlServerPersistenceProvider(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;", true, true);
        }
    }
}
