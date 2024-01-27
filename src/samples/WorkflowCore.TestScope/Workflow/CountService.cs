namespace WorkflowCore.TestScope.Workflow
{
    public class CountService
    {
        public int Count { get; private set; }

        public void Increment()
        {
            Count++;
        }
    }
}
