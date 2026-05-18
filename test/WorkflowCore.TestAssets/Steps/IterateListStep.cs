using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.TestAssets.Steps
{
    public class IterateListStep : Foreach
    {
        // This class inherits from Foreach and should be able to use
        // the RunParallel property from the base class in YAML definitions
    }
}