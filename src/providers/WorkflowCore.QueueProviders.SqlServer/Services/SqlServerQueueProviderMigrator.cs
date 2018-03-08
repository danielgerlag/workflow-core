#region using

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public interface ISqlServerQueueProviderMigrator
    {
        void MigrateDb();
        void CreateDb();
    }

    public class SqlServerQueueProviderMigrator : ISqlServerQueueProviderMigrator
    {
        private readonly string _connectionString;

        private readonly IBrokerNamesProvider _names;
        private readonly ISqlCommandExecutor _sqlCommandExecutor;

        public SqlServerQueueProviderMigrator(string connectionString, IBrokerNamesProvider names, ISqlCommandExecutor sqlCommandExecutor)
        {
            _connectionString = connectionString;

            _names = names;
            _sqlCommandExecutor = sqlCommandExecutor;
        }


        #region Migrate

        public void MigrateDb()
        {
            var cn = new SqlConnection(_connectionString);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                var n = new[]
                {
                    _names.GetByQueue(QueueType.Workflow),
                    _names.GetByQueue(QueueType.Event)
                };

                foreach (var item in n)
                {
                    CreateMessageType(cn, tx, item.MsgType);

                    CreateContract(cn, tx, item.ContractName, item.MsgType);

                    CreateQueue(cn, tx, item.QueueName);

                    CreateService(cn, tx, item.InitiatorService, item.QueueName, item.ContractName);
                    CreateService(cn, tx, item.TargetService, item.QueueName, item.ContractName);
                }
                
                tx.Commit();
            } finally
            {
                cn.Close();
            }
        }

        private void CreateService(SqlConnection cn, SqlTransaction tx, string name, string queueName, string contractName)
        {
            var cmdtext = @"select name from sys.services where name=@name";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.Parameters.AddWithValue("@name", name);

                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE SERVICE [{name}] ON QUEUE [{queueName}]([{contractName}]);";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateQueue(SqlConnection cn, SqlTransaction tx, string queueName)
        {
            var cmdtext = @"select name from sys.service_queues where name=@name";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.Parameters.AddWithValue("@name", queueName);

                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE QUEUE [{queueName}];";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateContract(SqlConnection cn, SqlTransaction tx, string contractName, string messageName)
        {
            var cmdtext = @"select name from sys.service_contracts where name=@name";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.Parameters.AddWithValue("@name", contractName);

                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE CONTRACT [{contractName}] ( [{messageName}] SENT BY INITIATOR);";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateMessageType(SqlConnection cn, SqlTransaction tx, string message)
        {
            var cmdtext = @"select name from sys.service_message_types where name=@name";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.Parameters.AddWithValue("@name", message);

                var n = (string)cmd.ExecuteScalar();

                if (!String.IsNullOrEmpty(n)) return;
            }

            cmdtext = $"CREATE MESSAGE TYPE [{message}] VALIDATION = NONE;";
            using (var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext))
            {
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        public void CreateDb()
        {
            var pattern = "Database=(.[^;]+);";

            var regex = new Regex(pattern);
            var db = regex.Match(_connectionString).Groups[1].Value;

            var masterCn = _connectionString.Replace(regex.Match(_connectionString).Groups[0].Value, "Database=master;");

            bool dbPresente;
            var cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();

                var cmd = cn.CreateCommand();
                cmd.CommandText = "select name from sys.databases where name = @dbname";
                cmd.Parameters.AddWithValue("@dbname", db);
                var found = cmd.ExecuteScalar();
                dbPresente = (found != null);
            } finally
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
                } finally
                {
                    cn.Close();
                }
            }

            EnableBroker(masterCn, db);
        }

        private void EnableBroker(string masterCn, string db)
        {
            var cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                var cmdtext = @"select is_broker_enabled from sys.databases where name = @name";
                
                var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext);
                cmd.Parameters.AddWithValue("@name", db);

                bool isBrokerEnabled = (bool)cmd.ExecuteScalar();
                if (isBrokerEnabled) return;

                tx.Commit();
            } finally
            {
                cn.Close();
            }

            cn = new SqlConnection(masterCn);
            try
            {
                cn.Open();
                var tx = cn.BeginTransaction();

                var cmdtext = $"ALTER DATABASE [{db}] SET ENABLE_BROKER;";
                var cmd = _sqlCommandExecutor.CreateCommand(cn, tx, cmdtext);

                cmd.ExecuteScalar();
                tx.Commit();
            } finally
            {
                cn.Close();
            }
        }
    }
}