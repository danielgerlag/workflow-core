# SQL Server sample

A sample to test SQL Server for persistence, locking and queueing.


```c#
services.AddWorkflow(x =>
                {
                    x.UseSqlServerQueue(_connectionString, "SampleSqlServer", true);
                    x.UseSqlServer(_connectionString, false, true);
                    x.UseSqlServerLocking(_connectionString);
                }
            );
```

It require a SQL Server database (tested with 2008R2 and 2016) available with this connection string:
    
        "Server=(local);Database=wfc;User Id=wfc;Password=wfc;"

and SQL Server Service Broker enabled (this command must be executed in single user mode).

```sql
	ALTER DATABASE wfc SET ENABLE_BROKER
	WITH ROLLBACK IMMEDIATE;
```
