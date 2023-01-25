#region using

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;
using WorkflowCore.QueueProviders.SqlServer.Models;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProvider : IQueueProvider
    {
        private readonly string _connectionString;

        private readonly bool _canMigrateDb;
        private readonly bool _canCreateDb;

        private readonly IQueueConfigProvider _configProvider;
        private readonly ISqlServerQueueProviderMigrator _migrator;
        private readonly ISqlCommandExecutor _sqlCommandExecutor;

        private readonly string _queueWorkCommand;
        private readonly string _dequeueWorkCommand;

        public SqlServerQueueProvider(
            SqlServerQueueProviderOptions opt, 
            IQueueConfigProvider configProvider, 
            ISqlServerQueueProviderMigrator migrator, 
            ISqlCommandExecutor sqlCommandExecutor)
        {
            _configProvider = configProvider;
            _migrator = migrator;
            _sqlCommandExecutor = sqlCommandExecutor;
            _connectionString = opt.ConnectionString;
            _canMigrateDb = opt.CanMigrateDb;
            _canCreateDb = opt.CanCreateDb;

            IsDequeueBlocking = true;

            _queueWorkCommand = GetFromResource("QueueWork");
            _dequeueWorkCommand = GetFromResource("DequeueWork");
        }

        private IEnumerable<QueueConfig> GetQueuesSortedByPriority(QueueType queue)
        {
            return _configProvider.GetAll()
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        private static string GetFromResource(string file)
        {
            var resName = $"WorkflowCore.QueueProviders.SqlServer.SqlCommands.{file}.sql";

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resName)))
            {
                return reader.ReadToEnd();
            }
        }


        public bool IsDequeueBlocking { get; }

#pragma warning disable CS1998

        public async Task Start()
        {
            if (_canCreateDb) await _migrator.CreateDbAsync();
            if (_canMigrateDb) await _migrator.MigrateDbAsync();
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
        public async Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id), "Param id must not be null");

            SqlConnection cn = new SqlConnection(_connectionString);
            try
            {
                await cn.OpenAsync();
                var par = _configProvider.GetByQueue(queue, priority);

                await _sqlCommandExecutor.ExecuteCommandAsync(cn, null, _queueWorkCommand,
                    new SqlParameter("@initiatorService", par.InitiatorService),
                    new SqlParameter("@targetService", par.TargetService),
                    new SqlParameter("@contractName", par.ContractName),
                    new SqlParameter("@msgType", par.MsgType),
                    new SqlParameter("@RequestMessage", id)
                    );
            }
            finally
            {
                cn.Close();
            }
        }


        /// <inheritdoc />
        /// <summary>
        /// Write a new id to the specified queue
        /// </summary>
        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        /// <inheritdoc />
        /// <summary>
        /// Get an id from the specified queue.
        /// </summary>
        /// <returns>Next id from queue, null if no message arrives in one second.</returns>
        private async Task<string> DequeueWork(QueueConfig config, CancellationToken cancellationToken)
        {
            SqlConnection cn = new SqlConnection(_connectionString);
            try
            {
                await cn.OpenAsync(cancellationToken);

                var sql = _dequeueWorkCommand.Replace("{queueName}", config.QueueName);
                var msg = await _sqlCommandExecutor.ExecuteScalarAsync<object>(cn, null, sql);
                return msg is DBNull ? null : (string)msg;

            }
            finally
            {
                cn.Close();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Get an id from the specified queue.
        /// </summary>
        /// <returns>Next id from queue, null if no message arrives in one second.</returns>
        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            foreach (var q in GetQueuesSortedByPriority(queue))
            {
                var result = await DequeueWork(q, cancellationToken);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}