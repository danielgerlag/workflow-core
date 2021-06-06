using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using System.Data;
using System.Collections.Generic;
using System.Threading;
using MySql.Data.MySqlClient;

namespace WorkflowCore.LockProviders.MySQL
{
    public class MySqlLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "wfc";

        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly Dictionary<string, MySqlConnection> _locks = new Dictionary<string, MySqlConnection>();
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);

        public MySqlLockProvider(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<MySqlLockProvider>();
            var csb = new MySqlConnectionStringBuilder(connectionString);
            csb.Pooling = true;
            _connectionString = csb.ToString();
        }


        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            if (_mutex.WaitOne())
            {
                try
                {
                    var connection = new MySqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"SELECT GET_LOCK('{Prefix}:{Id}', 10)";                                                
                        
                        var returnValue = await cmd.ExecuteScalarAsync(cancellationToken);

                        if (returnValue == null)
                        {
                            _logger.LogError($"Acquire lock provider error for {Id}");
                            return false;
                        }

                        var result = Convert.ToInt32(returnValue);                        
                        if (result > 0)
                        {
                            _logger.LogDebug($"Acquired lock for {Id}");
                            _locks[Id] = connection;
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"The acquire lock request timed out for {Id}");
                            connection.Close();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
                finally
                {
                    _mutex.Set();
                }
            }
            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            if (_mutex.WaitOne())
            {
                try
                {
                    MySqlConnection connection = null;

                    if (_locks.ContainsKey(Id))
                        connection = _locks[Id];
                                        
                    if (connection == null)
                    {
                        _logger.LogError($"Release lock connection not found for {Id}");
                        return;
                    }

                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"SELECT RELEASE_LOCK('{Prefix}:{Id}')";
                        cmd.CommandType = CommandType.Text;                        

                        var returnValue = await cmd.ExecuteScalarAsync();
                        if (returnValue == null)
                        {
                            _logger.LogError($"Release lock provider error for {Id}");                            
                        }

                        var result = Convert.ToInt32(returnValue);

                        if (result > 0)
                        {
                            _logger.LogDebug($"Released lock for {Id}");
                        }
                        else
                        {
                            _logger.LogError($"Release lock returned 0 for {Id}");
                            
                        }                        
                    }
                    finally
                    {
                        connection.Close();
                        _locks.Remove(Id);
                    }
                }
                finally
                {
                    _mutex.Set();
                }
            }
        }

        public Task Start() => Task.CompletedTask;
        
        public Task Stop() => Task.CompletedTask;
        
    }
}
