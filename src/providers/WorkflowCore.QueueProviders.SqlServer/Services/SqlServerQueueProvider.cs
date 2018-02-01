#region using

using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProvider : IQueueProvider
    {
        readonly string _connectionString;
        readonly string _workflowHostName;

        readonly bool _canMigrateDb;
        readonly bool _canCreateDb;

        readonly IBrokerNamesProvider _names;

        private readonly string _queueWork;
        private readonly string _dequeueWork;

        public SqlServerQueueProvider(string connectionString, string workflowHostName, bool canMigrateDb, bool canCreateDb)
        {
            _connectionString = connectionString;
            _workflowHostName = workflowHostName;
            _canMigrateDb = canMigrateDb;
            _canCreateDb = canCreateDb;
            _names = new BrokerNamesProvider(workflowHostName);

            IsDequeueBlocking = true;

            _queueWork = GetFromResource("QueueWork");
            _dequeueWork = GetFromResource("DequeueWork");
        }

        private static string GetFromResource(string file)
        {
            var resName = $"WorkflowCore.QueueProviders.SqlServer.Services.{file}.sql";

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resName)))
            {
                return reader.ReadToEnd();
            }
        }


        public bool IsDequeueBlocking { get; }

        public async Task Start()
        {
            var mig = new SqlServerQueueProviderMigrator(_connectionString, _workflowHostName);

            if (_canCreateDb) mig.CreateDb();
            if (_canMigrateDb) mig.MigrateDb();
        }

        public async Task Stop()
        {
            // Do nothing
        }

        public void Dispose()
        {
            Stop().Wait();
        }

        /// <inheritdoc />
        /// <summary>
        /// Write a new id to the specified queue
        /// </summary>
        /// <param name="id"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        public async Task QueueWork(string id, QueueType queue)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id), "Param id must not be null");

            SqlConnection cn = null;
            try
            {
                string msgType, initiatorService, targetService, contractName;
                if (queue == QueueType.Workflow)
                {
                    msgType = _names.WorkflowMessageType;
                    initiatorService = _names.InitiatorWorkflowServiceName;
                    targetService = _names.TargetWorkflowServiceName;
                    contractName = _names.WorkflowContractName;
                } else
                {
                    msgType = _names.EventMessageType;
                    initiatorService = _names.InitiatorEventServiceName;
                    targetService = _names.TargetEventServiceName;
                    contractName = _names.EventContractName;
                }

                var sql = _queueWork.Replace("{initiatorService}",initiatorService)
                    .Replace("{targetService}",targetService)
                    .Replace("{contractName}", contractName)
                    .Replace("{msgType}", msgType);

                cn = new SqlConnection(_connectionString);
                cn.Open();
                using (var cmd = SqlConnectionHelper.CreateCommand(cn, null, sql))
                {
                    cmd.Parameters.AddWithValue("@RequestMessage", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            } finally
            {
                cn?.Close();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Get an id from the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>Next id from queue, null if no message arrives in one second.</returns>
        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            SqlConnection cn = null;
            try
            {
                var queueName = queue == QueueType.Workflow ? _names.WorkflowQueueName : _names.EventQueueName;

                var sql = _dequeueWork.Replace("{queueName}", queueName);

                cn = new SqlConnection(_connectionString);
                cn.Open();
                using (var cmd = SqlConnectionHelper.CreateCommand(cn, null, sql))
                {
                    var msg = await cmd.ExecuteScalarAsync(cancellationToken);
                    return msg is DBNull ? null : (string)msg;
                }
            } finally
            {
                cn?.Close();
            }
        }
    }
}