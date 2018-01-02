# SQL Server sample

A sample to test SQL Server for persistenze, locking and queueing.


```c#
services.AddWorkflow(x =>
                {
                    x.UseSqlServerQueue(_connectionString, "SampleSqlServer", true);
                    x.UseSqlServer(_connectionString, false, true);
                    x.UseSqlServerLocking(_connectionString);
                }
            );
```

It require a SQL Server database available with this connection string:
    
        "Server=(local);Database=wfc;User Id=wfc;Password=wfc;"

and SQL Server Service Broker enabled.

```sql
	ALTER DATABASE wcf SET ENABLE_BROKER
	WITH ROLLBACK IMMEDIATE;
```