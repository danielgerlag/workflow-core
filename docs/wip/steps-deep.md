The first time a particular step within the workflow is called, the PersistenceData property on the context object is *null*.  The ExecutionResult produced by the Run method can either cause the workflow to proceed to the next step by providing an outcome value, instruct the workflow to sleep for a defined period or simply not move the workflow forward.  If no outcome value is produced, then the step becomes re-entrant by setting PersistenceData, so the workflow host will call this step again in the future buy will populate the PersistenceData with it's previous value.

For example, this step will initially run with *null* PersistenceData and put the workflow to sleep for 12 hours, while setting the PersistenceData to *new Object()*.  12 hours later, the step will be called again but context.PersistenceData will now contain the object constructed in the previous iteration, and will now produce an outcome value of *null*, causing the workflow to move forward.

```C#
public class SleepStep : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.PersistenceData == null)
            return ExecutionResult.Sleep(Timespan.FromHours(12), new Object());
        else
            return ExecutionResult.Next();
    }
}
```
