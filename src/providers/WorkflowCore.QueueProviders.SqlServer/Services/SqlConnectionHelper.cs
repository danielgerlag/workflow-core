#region using

using System;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlConnectionHelper
    {
        internal static SqlCommand CreateCommand(SqlConnection cn, SqlTransaction tx, string cmdtext, string name = null)
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