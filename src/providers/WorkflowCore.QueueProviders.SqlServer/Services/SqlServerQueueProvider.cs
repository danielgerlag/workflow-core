using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using WorkflowCore.Interface;

namespace WorkflowCore.QueueProviders.SqlServer.Services {
    public class SqlServerQueueProvider : IQueueProvider
    {
        readonly string _connectionString;
        readonly string _workflowHostName;
        readonly bool _canMigrateDb;
        SqlConnection _cn;

        public SqlServerQueueProvider(string connectionString, string workflowHostName, bool canMigrateDb) {
            _connectionString = connectionString;
            _workflowHostName = workflowHostName;
            _canMigrateDb = canMigrateDb;

            IsDequeueBlocking = true;
           
        }

        public void Dispose() {
            Stop().Wait();
        }

        public Task QueueWork(string id, QueueType queue) {
            throw new NotImplementedException();

            /*
             DECLARE @InitDlgHandle UNIQUEIDENTIFIER
            DECLARE @RequestMessage VARCHAR(1000) 
            BEGIN TRAN 

            --Determine the Initiator Service, Target Service and the Contract 
            BEGIN DIALOG @InitDlgHandle
            FROM SERVICE
            [//workflow-core/UnitTest/initiatorService]
            TO SERVICE
            '//workflow-core/UnitTest/targetService'
            ON CONTRACT
            [//workflow-core/UnitTest/contract]
            WITH ENCRYPTION=OFF; 

            --Prepare the Message
            SELECT @RequestMessage = N'<workflow> Send a Message to Target </workflow>'; 

            --Send the Message
            SEND ON CONVERSATION @InitDlgHandle 
            MESSAGE TYPE
            [//workflow-core/UnitTest/workflow]
            (@RequestMessage);

            SELECT @RequestMessage AS SentRequestMessage;
            COMMIT TRAN 

             */
        }

        public Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken) {
            throw new NotImplementedException();


            /*
             DECLARE @TargetDlgHandle UNIQUEIDENTIFIER
                DECLARE @ReplyMessage VARCHAR(1000)
                DECLARE @ReplyMessageName Sysname 
                BEGIN TRAN; 
                --Receive message from Initiator
                RECEIVE TOP(1)
                @TargetDlgHandle=Conversation_Handle
                ,@ReplyMessage=Message_Body
                ,@ReplyMessageName=Message_Type_Name
                FROM workflowcore_UnitTest; 

                SELECT @TargetDlgHandle, @ReplyMessage,@ReplyMessageName
                COMMIT TRAN 
             */
        }

        public bool IsDequeueBlocking { get; }

        public Task Start() {
            _cn = new SqlConnection(_connectionString);
            if (_canMigrateDb) {
                var mig = new SqlServerQueueProviderMigrator(_connectionString, _workflowHostName);
                mig.MigrateDb();
            }
            return Task.CompletedTask;
        }

        public Task Stop() {
            _cn.Close();
            return Task.CompletedTask;
        }
    }
}