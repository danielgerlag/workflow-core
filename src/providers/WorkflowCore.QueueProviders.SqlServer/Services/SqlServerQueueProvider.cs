#region using

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerQueueProvider : IQueueProvider
    {
        readonly string _connectionString;
        readonly string _workflowHostName;
        readonly bool _canMigrateDb;
        SqlConnection _cn;
        readonly SqlServerNames _names;
        //private ILogger _lg;

        public SqlServerQueueProvider(string connectionString, string workflowHostName, bool canMigrateDb/*, ILoggerFactory logFactory*/)
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
            _cn = new SqlConnection(_connectionString);

            if (_canMigrateDb)
            {
                var mig = new SqlServerQueueProviderMigrator(_connectionString, _workflowHostName);
                mig.MigrateDb();
            }
        }

        public async Task Stop()
        {
            _cn.Close();
        }

        public void Dispose()
        {
            Stop().Wait();
        }


        public async Task QueueWork(string id, QueueType queue)
        {
            var cn = new SqlConnection(_connectionString);
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

--Determine the Initiator Service, Target Service and the Contract 
BEGIN DIALOG @InitDlgHandle
FROM SERVICE
[{initiatorService}]
TO SERVICE
'{targetService}'
ON CONTRACT
[{contractName}]
WITH ENCRYPTION=OFF; 

--Send the Message
SEND ON CONVERSATION @InitDlgHandle 
MESSAGE TYPE
[{msgType}]
(@RequestMessage);

COMMIT TRAN
";


                cn.Open();
                using (var cmd = SqlConnectionHelper.CreateCommand(cn, null, sql))
                {
                    cmd.Parameters.AddWithValue("@RequestMessage", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            } finally
            {
                cn.Close();
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            var cn = new SqlConnection(_connectionString);
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
                
                cn.Open();
                using (var cmd = SqlConnectionHelper.CreateCommand(cn, null, sql))
                {
                    var msg = await cmd.ExecuteScalarAsync(cancellationToken);
                    return msg is DBNull ? null : (string)msg;
                }
            } finally
            {
                cn.Close();
            }
        }
    }
}