using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Persistence.SqlServer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServer(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB)
        {
            options.UsePersistence(sp => new SqlServerPersistenceProvider(connectionString, canCreateDB, canMigrateDB));
            return options;
        }
    }
}
