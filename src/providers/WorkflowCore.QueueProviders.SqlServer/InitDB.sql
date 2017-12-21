

-- Verifica attivazione SSSB ---------------------------------------------------------
-- Per disattivare			ALTER DATABASE MOSE SET DISABLE_BROKER;
-- Per generare nuovo id	alter database MOSE set NEW_BROKER

declare @b bit

select @b=is_broker_enabled
from sys.databases
where name='MOSE'

if @b=1 
begin
	print 'SSSB già attivo'
end
else
begin
	print 'Attivazione SSSB'
	ALTER DATABASE MOSE SET ENABLE_BROKER
	WITH ROLLBACK IMMEDIATE;
	print 'SSSB adesso è attivo'
end

GO



CREATE MESSAGE TYPE
[//workflow-core/{instance}/workflow]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE
[//workflow-core/{instance}/event]
VALIDATION = WELL_FORMED_XML;