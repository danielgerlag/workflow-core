using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

namespace WorkflowCore.LockProviders.SqlServer
{
    public class SqlLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "wfc";

        private readonly SqlConnection _connection;
        private readonly ILogger _logger;

        public SqlLockProvider(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<SqlLockProvider>();
            var csb = new SqlConnectionStringBuilder(connectionString);
            csb.Pooling = false;
            
            _connection = new SqlConnection(csb.ToString());
        }


        public async Task<bool> AcquireLock(string Id)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "EXEC @result = sp_getapplock @Resource = @id,  @LockMode = 'Exclusive', @LockOwner = 'Session'";
            cmd.Parameters.AddWithValue("id", $"{Prefix}:{Id}");
            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            switch (result)
            {
                case -1:
                    _logger.LogDebug($"The lock request timed out for {Id}");
                    break;
                case -2:
                    _logger.LogDebug($"The lock request was canceled for {Id}");
                    break;
                case -3:
                    _logger.LogDebug($"The lock request was chosen as a deadlock victim for {Id}");
                    break;
                case -999:
                    _logger.LogError($"Lock provider error for {Id}");
                    break;
            }

            return (result >= 0);
        }

        public async Task ReleaseLock(string Id)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "EXEC @result = sp_releaseapplock @Resource = @id, @LockOwner = 'Session'";
            cmd.Parameters.AddWithValue("id", $"{Prefix}:{Id}");
            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            
            if (result < 0)
                _logger.LogError($"Unable to release lock for {Id}");
        }

        public void Start()
        {
            _connection.Open();
        }

        public void Stop()
        {
            _connection.Close();
        }
    }
}
