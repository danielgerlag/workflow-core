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
        /// <param name="name">Add this value to parameter @name</param>
        /// <returns></returns>
        SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext, string name = null);
    }
}