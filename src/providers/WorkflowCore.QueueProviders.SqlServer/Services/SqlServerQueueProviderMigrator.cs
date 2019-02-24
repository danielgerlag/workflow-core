#region using

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

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

        public void MigrateDb()
        {
            var cn = new SqlConnection(_connectionString);
            cn.Open();
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
                    CreateMessageType(cn, tx, item.MsgType);

                    CreateContract(cn, tx, item.ContractName, item.MsgType);

                    CreateQueue(cn, tx, item.QueueName);

                    CreateService(cn, tx, item.InitiatorService, item.QueueName, item.ContractName);
                    CreateService(cn, tx, item.TargetService, item.QueueName, item.ContractName);
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

        private void CreateService(SqlConnection cn, SqlTransaction tx, string name, string queueName, string contractName)
        {
            var cmdtext = @"select name from sys.services where name=@name";
            var existing = _sqlCommandExecutor.ExecuteScalar<string>(cn, tx, cmdtext, new SqlParameter("@name", name));

            if (!string.IsNullOrEmpty(existing))
                return;
            
            _sqlCommandExecutor.ExecuteCommand(cn, tx, $"CREATE SERVICE [{name}] ON QUEUE [{queueName}]([{contractName}]);");
        }

        private void CreateQueue(SqlConnection cn, SqlTransaction tx, string queueName)
        {
            var cmdtext = @"select name from sys.service_queues where name=@name";
            var existing = _sqlCommandExecutor.ExecuteScalar<string>(cn, tx, cmdtext, new SqlParameter("@name", queueName));

            if (!string.IsNullOrEmpty(existing))
                return;
                        
            _sqlCommandExecutor.ExecuteCommand(cn, tx, $"CREATE QUEUE [{queueName}];");
        }

        private void CreateContract(SqlConnection cn, SqlTransaction tx, string contractName, string messageName)
        {
            var cmdtext = @"select name from sys.service_contracts where name=@name";
            var existing = _sqlCommandExecutor.ExecuteScalar<string>(cn, tx, cmdtext, new SqlParameter("@name", contractName));

            if (!string.IsNullOrEmpty(existing))
                return;
                        
            _sqlCommandExecutor.ExecuteCommand(cn, tx, $"CREATE CONTRACT [{contractName}] ( [{messageName}] SENT BY INITIATOR);");
        }

        private void CreateMessageType(SqlConnection cn, SqlTransaction tx, string message)
        {
            var cmdtext = @"select name from sys.service_message_types where name=@name";
            var existing = _sqlCommandExecutor.ExecuteScalar<string>(cn, tx, cmdtext, new SqlParameter("@name", message));

            if (!string.IsNullOrEmpty(existing))
                return;
            
            _sqlCommandExecutor.ExecuteCommand(cn, tx, $"CREATE MESSAGE TYPE [{message}] VALIDATION = NONE;");
        }

        #endregion

        public void CreateDb()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var masterBuilder = new SqlConnectionStringBuilder(_connectionString);
            masterBuilder.InitialCatalog = "master";

            var masterCnStr = masterBuilder.ToString();

            bool dbPresente;
            var cn = new SqlConnection(masterCnStr);
            cn.Open();
            try
            {
                var cmd = cn.CreateCommand();
                cmd.CommandText = "select name from sys.databases where name = @dbname";
                cmd.Parameters.AddWithValue("@dbname", builder.InitialCatalog);
                var found = cmd.ExecuteScalar();
                dbPresente = (found != null);

                if (!dbPresente)
                {   
                    var createCmd = cn.CreateCommand();
                    createCmd.CommandText = "create database [" + builder.InitialCatalog + "]";
                    createCmd.ExecuteNonQuery();
                }
            }
            finally
            {
                cn.Close();
            }            

            EnableBroker(masterCnStr, builder.InitialCatalog);
        }

        private void EnableBroker(string masterCn, string db)
        {
            var cn = new SqlConnection(masterCn);
            cn.Open();

            var isBrokerEnabled = _sqlCommandExecutor.ExecuteScalar<bool>(cn, null, @"select is_broker_enabled from sys.databases where name = @name", new SqlParameter("@name", db));

            if (isBrokerEnabled)
                return;

            var tx = cn.BeginTransaction();
            try
            {
                _sqlCommandExecutor.ExecuteCommand(cn, tx, $"ALTER DATABASE [{db}] SET ENABLE_BROKER;");
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