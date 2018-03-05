#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public interface ISqlCommandExecutor
    {
        /// <summary>
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="tx"></param>
        /// <param name="cmdtext"></param>
        /// <returns></returns>
        SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext);
    }
}