using System;

namespace WorkflowCore.EventBus.Abstractions
{
    public abstract class IntegrationEvent
    {
        protected IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        public Guid Id  { get; }
        public DateTime CreationDate { get; }
    }
}
