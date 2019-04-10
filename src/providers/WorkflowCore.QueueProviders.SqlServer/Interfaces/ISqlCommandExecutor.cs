#region using

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Interfaces
{
    public interface ISqlCommandExecutor
    {
        Task<TResult> ExecuteScalarAsync<TResult>(SqlConnection cn, SqlTransaction tx, string cmdtext, params DbParameter[] parameters);
        Task<int> ExecuteCommandAsync(SqlConnection cn, SqlTransaction tx, string cmdtext, params DbParameter[] parameters);
    }
}