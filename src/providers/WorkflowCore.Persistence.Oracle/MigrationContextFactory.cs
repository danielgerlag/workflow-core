using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowCore.Persistence.Oracle
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<OracleContext>
    {
        public OracleContext CreateDbContext(string[] args)
        {
            return new OracleContext(@"Server=127.0.0.1;Database=myDataBase;Uid=myUsername;Pwd=myPassword;");
        }
    }
}
