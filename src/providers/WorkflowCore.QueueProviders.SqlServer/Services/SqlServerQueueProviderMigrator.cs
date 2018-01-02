#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProviderMigrator
    {
        readonly string _connectionString;

        readonly SqlServerNames _names;

        public SqlServerQueueProviderMigrator(string connectionString, string workflowHostName)
        {
            _connectionString = connectionString;

            _names = new SqlServerNames(workflowHostName);
        }


        internal void MigrateDb()
        {
            var cn = new SqlConnection(_connectionString);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                EnableBroker(cn, tx);
                CreateMessageType(cn, tx, _names.WorkflowMessageType);
                CreateMessageType(cn, tx, _names.EventMessageType);

                CreateContract(cn, tx, _names.EventContractName, _names.EventMessageType);
                CreateContract(cn, tx, _names.WorkflowContractName, _names.WorkflowMessageType);

                CreateQueue(cn, tx, _names.EventQueueName);
                CreateQueue(cn, tx, _names.WorkflowQueueName);

                CreateService(cn, tx, _names.InitiatorEventServiceName, _names.EventQueueName, _names.EventContractName);
                CreateService(cn, tx, _names.TargetEventServiceName, _names.EventQueueName, _names.EventContractName);

                CreateService(cn, tx, _names.InitiatorWorkflowServiceName, _names.WorkflowQueueName, _names.WorkflowContractName);
                CreateService(cn, tx, _names.TargetWorkflowServiceName, _names.WorkflowQueueName, _names.WorkflowContractName);

                tx.Commit();
            } finally
            {
                cn.Close();
            }
        }

        private static void CreateService(SqlConnection cn, SqlTransaction tx, string name, string queueName, string contractName)
        {
            var cmdtext = @"select name from sys.services where name=@name";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, name))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE SERVICE [{name}] ON QUEUE [{queueName}]([{contractName}]);";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateQueue(SqlConnection cn, SqlTransaction tx, string queueName)
        {
            var cmdtext = @"select name from sys.service_queues where name=@name";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, queueName))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE QUEUE [{queueName}];";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateContract(SqlConnection cn, SqlTransaction tx, string contractName, string messageName)
        {
            var cmdtext = @"select name from sys.service_contracts where name=@name";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, contractName))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE CONTRACT [{contractName}] ( [{messageName}] SENT BY INITIATOR);";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateMessageType(SqlConnection cn, SqlTransaction tx, string message)
        {
            var cmdtext = @"select name from sys.service_message_types where name=@name";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, message))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE MESSAGE TYPE [{message}] VALIDATION = NONE;";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnableBroker(SqlConnection cn, SqlTransaction tx)
        {
            var cmdtext = @"select is_broker_enabled from sys.databases where name = @name";
            using (var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, cn.Database))
            {
                bool isBrokerEnabled = (bool)cmd.ExecuteScalar();
                if (isBrokerEnabled) return;
            }

            var msg =
                $"Service Broker not enabled on database {cn.Database}. Execute 'ALTER DATABASE {cn.Database} SET ENABLE_BROKER' in single user mode ";
            throw new InvalidOperationException(msg);
        }
    }
}