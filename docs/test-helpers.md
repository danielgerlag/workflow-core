# Test helpers for Workflow Core

Provides support writing tests for workflows built on WorkflowCore

## Installing

Install the NuGet package "WorkflowCore.Testing"

```
PM> Install-Package WorkflowCore.Testing
```

## Usage

### With xUnit

* Create a class that inherits from WorkflowTest
* Call the Setup() method in the constructor
* Implement your tests using the helper methods
	* StartWorkflow()
	* WaitForWorkflowToComplete()
	* WaitForEventSubscription()
	* GetStatus()
	* GetData()
	* UnhandledStepErrors

```C#
public class xUnitTest : WorkflowTest<MyWorkflow, MyDataClass>
{
    public xUnitTest()
    {
        Setup();
    }

    [Fact]
    public void MyWorkflow()
    {
        var workflowId = StartWorkflow(new MyDataClass() { Value1 = 2, Value2 = 3 });
        WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

        GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        UnhandledStepErrors.Count.Should().Be(0);
        GetData(workflowId).Value3.Should().Be(5);
    }
}
```


### With NUnit

* Create a class that inherits from WorkflowTest and decorate it with the *TestFixture* attribute
* Override the Setup method and decorate it with the *SetUp* attribute
* Implement your tests using the helper methods
	* StartWorkflow()
	* WaitForWorkflowToComplete()
	* WaitForEventSubscription()
	* GetStatus()
	* GetData()
	* UnhandledStepErrors

```C#
[TestFixture]
public class NUnitTest : WorkflowTest<MyWorkflow, MyDataClass>
{
    [SetUp]
    protected override void Setup()
    {
        base.Setup();
    }

    [Test]
    public void NUnit_workflow_test_sample()
    {
        var workflowId = StartWorkflow(new MyDataClass() { Value1 = 2, Value2 = 3 });
        WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

        GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        UnhandledStepErrors.Count.Should().Be(0);
        GetData(workflowId).Value3.Should().Be(5);
    }

}
```
