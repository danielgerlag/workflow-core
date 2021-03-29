#region using

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{    

    public class SqlServerQueueProviderMigrator : ISqlServerQueueProviderMigrator
    {
        private readonly string _connectionString;

        private readonly IQueueConfigProvider _configProvider;
        private readonly ISqlCommandExecutor _sqlCommandExecutor;

        public SqlServerQueueProviderMigrator(string connectionString, IQueueConfigProvider configProvider, ISqlCommandExecutor sqlCommandExecutor)
        {
            _connectionString = connectionString;
            _configProvider = configProvider;
            _sqlCommandExecutor = sqlCommandExecutor;
        }


        #region Migrate

        public async Task MigrateDbAsync()
        {
            var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();
            var tx = cn.BeginTransaction();
            try
            {
                var queueConfigurations = new[]
                {
                    _configProvider.GetByQueue(QueueType.Workflow),
                    _configProvider.GetByQueue(QueueType.Event),
                    _configProvider.GetByQueue(QueueType.Index)
                };

                foreach (var item in queueConfigurations)
                {
                    await CreateMessageType(cn, tx, item.MsgType);

                    await CreateContract(cn, tx, item.ContractName, item.MsgType);

                    await CreateQueue(cn, tx, item.QueueName);

                    await CreateService(cn, tx, item.InitiatorService, item.QueueName, item.ContractName);
                    await CreateService(cn, tx, item.TargetService, item.QueueName, item.ContractName);
                }
                
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            finally
            {
                cn.Close();
            }
        }

        private async Task CreateService(SqlConnection cn, SqlTransaction tx, string name, string queueName, string contractName)
        {
            var cmdtext = @"select name from sys.services where name=@name";
            var existing = await _sqlCommandExecutor.ExecuteScalarAsync<string>(cn, tx, cmdtext, new SqlParameter("@name", name));

            if (!string.IsNullOrEmpty(existing))
                return;
            
            await _sqlCommandExecutor.ExecuteCommandAsync(cn, tx, $"CREATE SERVICE [{name}] ON QUEUE [{queueName}]([{contractName}]);");
        }

        private async Task CreateQueue(SqlConnection cn, SqlTransaction tx, string queueName)
        {
            var cmdtext = @"select name from sys.service_queues where name=@name";
            var existing = await _sqlCommandExecutor.ExecuteScalarAsync<string>(cn, tx, cmdtext, new SqlParameter("@name", queueName));

            if (!string.IsNullOrEmpty(existing))
                return;
                        
            await _sqlCommandExecutor.ExecuteCommandAsync(cn, tx, $"CREATE QUEUE [{queueName}];");
        }

        private async Task CreateContract(SqlConnection cn, SqlTransaction tx, string contractName, string messageName)
        {
            var cmdtext = @"select name from sys.service_contracts where name=@name";
            var existing = await _sqlCommandExecutor.ExecuteScalarAsync<string>(cn, tx, cmdtext, new SqlParameter("@name", contractName));

            if (!string.IsNullOrEmpty(existing))
                return;
                        
            await _sqlCommandExecutor.ExecuteCommandAsync(cn, tx, $"CREATE CONTRACT [{contractName}] ( [{messageName}] SENT BY INITIATOR);");
        }

        private async Task CreateMessageType(SqlConnection cn, SqlTransaction tx, string message)
        {
            var cmdtext = @"select name from sys.service_message_types where name=@name";
            var existing = await _sqlCommandExecutor.ExecuteScalarAsync<string>(cn, tx, cmdtext, new SqlParameter("@name", message));

            if (!string.IsNullOrEmpty(existing))
                return;
            
            await _sqlCommandExecutor.ExecuteCommandAsync(cn, tx, $"CREATE MESSAGE TYPE [{message}] VALIDATION = NONE;");
        }

        #endregion

        public async Task CreateDbAsync()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var masterBuilder = new SqlConnectionStringBuilder(_connectionString);
            masterBuilder.InitialCatalog = "master";

            var masterCnStr = masterBuilder.ToString();

            bool dbPresente;
            var cn = new SqlConnection(masterCnStr);
            await cn.OpenAsync();
            try
            {
                var cmd = cn.CreateCommand();
                cmd.CommandText = "select name from sys.databases where name = @dbname";
                cmd.Parameters.AddWithValue("@dbname", builder.InitialCatalog);
                var found = await cmd.ExecuteScalarAsync();
                dbPresente = (found != null);

                if (!dbPresente)
                {   
                    var createCmd = cn.CreateCommand();
                    createCmd.CommandText = "create database [" + builder.InitialCatalog + "]";
                    await createCmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                cn.Close();
            }            

            await EnableBroker(masterCnStr, builder.InitialCatalog);
        }

        private async Task EnableBroker(string masterCn, string db)
        {
            var cn = new SqlConnection(masterCn);
            await cn.OpenAsync();

            var isBrokerEnabled = await _sqlCommandExecutor.ExecuteScalarAsync<bool>(cn, null, @"select is_broker_enabled from sys.databases where name = @name", new SqlParameter("@name", db));

            if (isBrokerEnabled)
                return;

            var tx = cn.BeginTransaction();
            try
            {
                await _sqlCommandExecutor.ExecuteCommandAsync(cn, tx, $"ALTER DATABASE [{db}] SET ENABLE_BROKER;");
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            finally
            {
                cn.Close();
            }            
        }
    }
}