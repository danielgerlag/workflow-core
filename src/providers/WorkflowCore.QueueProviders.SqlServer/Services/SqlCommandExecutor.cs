#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public interface ISqlCommandExecutor {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="tx"></param>
        /// <param name="cmdtext"></param>
        /// <param name="name">Add this value to parameter @name</param>
        /// <returns></returns>
        SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext, string name = null);
    }

    public class SqlCommandExecutor : ISqlCommandExecutor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="tx"></param>
        /// <param name="cmdtext"></param>
        /// <param name="name">Add this value to parameter @name</param>
        /// <returns></returns>
        public SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext, string name = null)
        {
            var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = cmdtext;
            if (name != null)
            {
                cmd.Parameters.AddWithValue("name", name);
            }

            return cmd;
        }
    }
}