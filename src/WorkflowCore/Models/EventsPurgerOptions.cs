using System;

namespace WorkflowCore.Models
{
    public class EventsPurgerOptions
    {
        public int BatchSize { get; } = 100;
        public int DeleteCommandTimeoutSeconds { get; } = 60;

        public EventsPurgerOptions(int batchSize, int deleteCommandTimeoutSeconds = 60) 
        {
            if (batchSize < 0)
                throw new ArgumentOutOfRangeException("Batch size shoud be greater than 0");

            if (deleteCommandTimeoutSeconds < 0)
                throw new ArgumentOutOfRangeException("Timeout shoud be greater than 0");

            BatchSize = batchSize;
            DeleteCommandTimeoutSeconds = deleteCommandTimeoutSeconds;
        }
    }
}
