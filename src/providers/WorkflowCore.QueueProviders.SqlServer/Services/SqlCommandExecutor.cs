#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlCommandExecutor : ISqlCommandExecutor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="tx"></param>
        /// <param name="cmdtext"></param>
        /// <returns></returns>
        public SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext)
        {
            var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = cmdtext;

            return cmd;
        }
    }
}