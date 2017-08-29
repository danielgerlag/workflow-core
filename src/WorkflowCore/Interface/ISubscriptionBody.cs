namespace WorkflowCore.Interface
{
    public interface ISubscriptionBody : IStepBody
    {
        object EventData { get; set; }        
    }
}
