using System;
using System.Threading.Tasks;

namespace WorkflowCore.Providers.AWS.Interface
{
    public interface IKinesisTracker
    {
        Task<string> GetNextShardIterator(string app, string stream, string shard);
        Task<string> GetNextLastSequenceNumber(string app, string stream, string shard);
        Task IncrementShardIterator(string app, string stream, string shard, string iterator);
        Task IncrementShardIteratorAndSequence(string app, string stream, string shard, string iterator, string sequence);
    }
}
