#region using

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlCommandExecutor : ISqlCommandExecutor
    {
        public async Task<TResult> ExecuteScalarAsync<TResult>(SqlConnection cn, SqlTransaction tx, string cmdtext, params DbParameter[] parameters)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = cmdtext;                

                foreach (var param in parameters)
                    cmd.Parameters.Add(param);
                
                return (TResult)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<int> ExecuteCommandAsync(SqlConnection cn, SqlTransaction tx, string cmdtext, params DbParameter[] parameters)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = cmdtext;

                foreach (var param in parameters)
                    cmd.Parameters.Add(param);

                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}