#region using

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProviderMigrator
    {
        readonly string _connectionString;

        readonly IBrokerNamesProvider _names;

        public SqlServerQueueProviderMigrator(string connectionString, string workflowHostName)
        {
            _connectionString = connectionString;

            _names = new BrokerNamesProvider(workflowHostName);
        }


        #region Migrate

        internal void MigrateDb()
        {
            var cn = new SqlConnection(_connectionString);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

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

        #endregion

        public void CreateDb()
        {
            var pattern = ";Database=(.[^;]+);";

            var regex = new Regex(pattern);
            var db = regex.Match(_connectionString).Groups[1].Value;

            var masterCn = _connectionString.Replace(db, "master");

            bool dbPresente;
            var cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();

                var cmd = cn.CreateCommand();
                cmd.CommandText = "select name from sys.databases where name = @dbname";
                cmd.Parameters.AddWithValue("@dbname", db);
                var found=cmd.ExecuteScalar();
                dbPresente = (found != null);
            }
            finally
            {
                cn.Close();
            }

            if (!dbPresente)
            {
                cn = new SqlConnection(masterCn);
                try
                {
                    cn.Open();
                    
                    var cmd = cn.CreateCommand();
                    cmd.CommandText = "create database [" + db + "]";
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cn.Close();
                }
            }

            EnableBroker(masterCn, db);
        }

        private static void EnableBroker(string masterCn, string db)
        {
            var cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                var cmdtext = @"select is_broker_enabled from sys.databases where name = @name";
                var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext, db);

                bool isBrokerEnabled = (bool)cmd.ExecuteScalar();
                if (isBrokerEnabled) return;

                tx.Commit();
            }
            finally
            {
                cn.Close();
            }

            cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                var cmdtext = $"ALTER DATABASE [{db}] SET ENABLE_BROKER;";
                var cmd = SqlConnectionHelper.CreateCommand(cn, tx, cmdtext);

                cmd.ExecuteScalar();
                tx.Commit();
            }
            finally
            {
                cn.Close();
            }
        }
    }
}