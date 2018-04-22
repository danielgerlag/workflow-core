#region using

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Interfaces
{
    public interface ISqlCommandExecutor
    {
        TResult ExecuteScalar<TResult>(IDbConnection cn, IDbTransaction tx, string cmdtext, params DbParameter[] parameters);
        int ExecuteCommand(IDbConnection cn, IDbTransaction tx, string cmdtext, params DbParameter[] parameters);
    }
}