using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Persistence.Sqlite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlite(this WorkflowOptions options, string connectionString, bool canCreateDB)
        {
            options.UsePersistence(sp => new SqlitePersistenceProvider(connectionString, canCreateDB));
            return options;
        }
    }
}
