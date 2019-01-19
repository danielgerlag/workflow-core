using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowCore.Providers.AWS.Interface
{
    public interface IKinesisTracker
    {
        Task<string> GetNextShardIterator(string app, string stream, string shard);
        Task IncrementShardIterator(string app, string stream, string shard, string iterator);
    }
}
