using MongoDB.Driver;
using WorkflowCore.Interface;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoTransaction : ITransaction
    {
        public MongoTransaction(IClientSessionHandle session)
        {
            Session = session;
        }

        public object Session { get; }
    }
}