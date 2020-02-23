using System;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowCore.Persistence.MySQL
{
    public class MigrationContextFactory : IDesignTimeDbContextFactory<MysqlContext>
    {
        public MysqlContext CreateDbContext(string[] args)
        {
            return new MysqlContext(@"Server=127.0.0.1;Database=myDataBase;Uid=myUsername;Pwd=myPassword;");
        }
    }
}
