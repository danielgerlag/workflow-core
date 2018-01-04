#region using

using System;
using System.Data.SqlClient;
using System.Linq;
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

        readonly SqlServerNames _names;
        //private ILogger _lg;

        public SqlServerQueueProvider(string connectionString, string workflowHostName, bool canMigrateDb /*, ILoggerFactory logFactory*/)
        {
            //_lg = logFactory.CreateLogger<SqlServerQueueProvider>();

            _connectionString = connectionString;
            _workflowHostName = workflowHostName;
            _canMigrateDb = canMigrateDb;
            _names = new SqlServerNames(workflowHostName);

            IsDequeueBlocking = true;
        }

        public bool IsDequeueBlocking { get; }

        public async Task Start()
        {
            if (_canMigrateDb)
            {
                var mig = new SqlServerQueueProviderMigrator(_connectionString, _workflowHostName);
                mig.MigrateDb();
            }
        }

        public async Task Stop()
        {
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

                var sql = $@"
DECLARE @InitDlgHandle UNIQUEIDENTIFIER
BEGIN TRAN 

BEGIN DIALOG @InitDlgHandle
FROM SERVICE
[{initiatorService}]
TO SERVICE
'{targetService}'
ON CONTRACT
[{contractName}]
WITH ENCRYPTION=OFF; 

SEND ON CONVERSATION @InitDlgHandle 
MESSAGE TYPE [{msgType}]
(@RequestMessage);

COMMIT TRAN
";

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

                var sql = $@"
DECLARE @TargetDlgHandle UNIQUEIDENTIFIER
DECLARE @Message varbinary(max)
DECLARE @MessageName Sysname 

BEGIN TRAN; 

WAITFOR (
    RECEIVE TOP(1)
    @TargetDlgHandle=Conversation_Handle
    ,@Message=Message_Body
    ,@MessageName=Message_Type_Name
    FROM [{queueName}]),  
TIMEOUT 1000;   

SELECT cast(@Message as nvarchar(max))
COMMIT TRAN 
";

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