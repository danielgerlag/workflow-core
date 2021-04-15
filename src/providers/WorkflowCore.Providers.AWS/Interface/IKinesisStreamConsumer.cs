using System;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;

namespace WorkflowCore.Providers.AWS.Interface
{
    public interface IKinesisStreamConsumer
    {
        Task Subscribe(string appName, string stream, Action<Record> action);
    }
}
