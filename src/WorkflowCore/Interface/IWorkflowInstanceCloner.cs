using System;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowInstanceCloner
    {
        /// <summary>
        /// Deep-clones a workflow instance for forking. Only active/pending execution pointers
        /// and their scope chain are included. All pointer IDs are remapped to new GUIDs.
        /// </summary>
        /// <param name="source">The source workflow instance to clone.</param>
        /// <param name="dataMutator">Optional callback to mutate the cloned data.</param>
        /// <returns>The cloned instance and any event subscriptions that need to be created.</returns>
        (WorkflowInstance clone, List<EventSubscription> subscriptions) CloneForFork(
            WorkflowInstance source,
            Action<object> dataMutator = null);
    }
}
