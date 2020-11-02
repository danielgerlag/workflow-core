using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    /// <summary>
    /// Represents a function that executes before or after a workflow starts or completes.
    /// </summary>
    public delegate Task WorkflowDelegate();
}
