#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services {
    public class SqlServerQueueProviderMigrator {
        readonly string _connectionString;

        readonly string _workflowMessageType;
        readonly string _eventMessageType;
        readonly string _contractName;
        readonly string _queueName;
        readonly string _initiatorServiceName;
        readonly string _targetServiceName;

        public SqlServerQueueProviderMigrator(string connectionString, string workflowHostName) {
            _connectionString = connectionString;

            _workflowMessageType = $"//workflow-core/{workflowHostName}/workflow";
            _eventMessageType = $"//workflow-core/{workflowHostName}/event";
            _contractName = $"//workflow-core/{workflowHostName}/contract";
            _initiatorServiceName = $"//workflow-core/{workflowHostName}/initiatorService";
            _targetServiceName = $"//workflow-core/{workflowHostName}/targetService";
            _queueName = $"workflowcore_{workflowHostName}";
        }

        internal void MigrateDb() {
            var cn = new SqlConnection(_connectionString);
            try {
                cn.Open();
                var tx = cn.BeginTransaction();

                EnableBroker(cn,tx);
                CreateMessageType(cn, tx, _workflowMessageType);
                CreateMessageType(cn, tx, _eventMessageType);
                CreateContract(cn, tx);
                CreateQueue(cn, tx);
                CreateService(cn, tx, _initiatorServiceName);
                CreateService(cn, tx, _targetServiceName);

                tx.Commit();
            }
            finally {
                cn.Close();
            }
        }

        void CreateService(SqlConnection cn, SqlTransaction tx, string serviceName) {
            var cmdtext = @"select name from sys.services where name=@name";
            using (var cmd = CreateCommand(cn, tx, cmdtext, serviceName))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE SERVICE [{serviceName}] ON QUEUE {_queueName};";
            using (var cmd = CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        void CreateQueue(SqlConnection cn, SqlTransaction tx) {
            var cmdtext = @"select name from sys.service_queues where name=@name";
            using (var cmd = CreateCommand(cn, tx, cmdtext, _queueName))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE QUEUE {_queueName};";
            using (var cmd = CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }

        }

        void CreateContract(SqlConnection cn, SqlTransaction tx) {

            var cmdtext = @"select name from sys.service_contracts where name=@name";
            using (var cmd = CreateCommand(cn, tx, cmdtext, _contractName))
            {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE CONTRACT [{_contractName}] ( [{_workflowMessageType}] SENT BY INITIATOR, [{_eventMessageType}] SENT BY INITIATOR);";
            using (var cmd = CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        void CreateMessageType(SqlConnection cn, SqlTransaction tx, string message) {

            var cmdtext = @"select name from sys.service_message_types where name=@name";
            using (var cmd = CreateCommand(cn, tx, cmdtext, message)) {
                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE MESSAGE TYPE [{message}] VALIDATION = WELL_FORMED_XML;";
            using (var cmd = CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        void EnableBroker(SqlConnection cn, SqlTransaction tx) {

            var cmdtext = @"select is_broker_enabled from sys.databases where name = @name";
            using (var cmd = CreateCommand(cn, tx, cmdtext, cn.Database))
            {
                bool isBrokerEnabled = (bool)cmd.ExecuteScalar();
                if (isBrokerEnabled) return;
            }

            var msg = $"Service Broker not enabled on database {cn.Database}. Execute 'ALTER DATABASE {cn.Database} SET ENABLE_BROKER' in single user mode ";
            throw new InvalidOperationException(msg);
        }

        static SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext, string name=null) {
            var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = cmdtext;
            if (name != null) {
                cmd.Parameters.AddWithValue("name", name);
            }
            return cmd;
        }
    }
}