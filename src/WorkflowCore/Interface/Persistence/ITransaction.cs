namespace WorkflowCore.Interface
{
    public interface ITransaction
    {
        /// <summary>
        /// The transaction session specific for each storage provider.
        /// </summary>
        object Session { get; }
    }
}