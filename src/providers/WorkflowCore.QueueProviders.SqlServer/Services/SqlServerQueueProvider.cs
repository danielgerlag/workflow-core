#region using

using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProvider : IQueueProvider
    {
        readonly string _connectionString;

        readonly bool _canMigrateDb;
        readonly bool _canCreateDb;

        private readonly IBrokerNamesProvider _names;
        private readonly ISqlServerQueueProviderMigrator _migrator;
        private readonly ISqlCommandExecutor _sqlCommandExecutor;

        private readonly string _queueWork;
        private readonly string _dequeueWork;

        public SqlServerQueueProvider(IServiceProvider serviceProvider, SqlServerQueueProviderOption opt)
        {
            _connectionString = opt.ConnectionString;
            _canMigrateDb = opt.CanMigrateDb;
            _canCreateDb = opt.CanCreateDb;

            _names = serviceProvider.GetService<IBrokerNamesProvider>()
                     ?? new BrokerNamesProvider(opt.WorkflowHostName);
            _sqlCommandExecutor = serviceProvider.GetService<ISqlCommandExecutor>()
                                  ?? new SqlCommandExecutor();
            _migrator = serviceProvider.GetService<ISqlServerQueueProviderMigrator>()
                        ?? new SqlServerQueueProviderMigrator(opt.ConnectionString, _names, _sqlCommandExecutor);

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

#pragma warning disable CS1998

        public async Task Start()
        {
            if (_canCreateDb) _migrator.CreateDb();
            if (_canMigrateDb) _migrator.MigrateDb();
        }

        public async Task Stop()
        {
            // Do nothing
        }

#pragma warning restore CS1998

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

                cn = new SqlConnection(_connectionString);
                cn.Open();
                using (var cmd = _sqlCommandExecutor.CreateCommand(cn, null, _queueWork))
                {
                    cmd.Parameters.AddWithValue("@initiatorService", initiatorService);
                    cmd.Parameters.AddWithValue("@targetService", targetService);
                    cmd.Parameters.AddWithValue("@contractName", contractName);
                    cmd.Parameters.AddWithValue("@msgType", msgType);
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
                using (var cmd = _sqlCommandExecutor.CreateCommand(cn, null, sql))
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