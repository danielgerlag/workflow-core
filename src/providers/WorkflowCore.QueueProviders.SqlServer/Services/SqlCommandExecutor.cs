#region using

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlCommandExecutor : ISqlCommandExecutor
    {
        public TResult ExecuteScalar<TResult>(IDbConnection cn, IDbTransaction tx, string cmdtext, params DbParameter[] parameters)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = cmdtext;                

                foreach (var param in parameters)
                    cmd.Parameters.Add(param);
                
                return (TResult)cmd.ExecuteScalar();
            }
        }

        public int ExecuteCommand(IDbConnection cn, IDbTransaction tx, string cmdtext, params DbParameter[] parameters)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = cmdtext;

                foreach (var param in parameters)
                    cmd.Parameters.Add(param);

                return cmd.ExecuteNonQuery();
            }
        }

        private IDbCommand CreateCommand(IDbConnection cn, IDbTransaction tx, string cmdtext)
        {
            var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = cmdtext;

            return cmd;
        }
    }
}