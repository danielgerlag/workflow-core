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