using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.Tests.YmalDefinition
{
    public class YmalDefinitionTest:YamlWorkflowTest
    {

        [Fact]
        public void LoadWorkflowDefinitions_NoException()
        {
            Setup();

            MyDataClass myData = new MyDataClass() { 
                Value1 = 1, 
                Value2 = 2, 
                Dict = new Dictionary<string, int>(), 
                anotherData = new AnotherDataClass() { Value3 = 3 } 
            };
            myData.Dict["testKey"] = 5; 

            String workflowInstanceId=StartWorkflow(File.ReadAllText("HelloWorld.yml"), myData); 
            WaitForWorkflowToComplete(workflowInstanceId, TimeSpan.FromSeconds(30));
            MyDataClass resultData = GetData<MyDataClass>(workflowInstanceId);
            Assert.Equal(10, resultData.Value1);
            Assert.Equal(11, resultData.Value2);
            Assert.Equal(100121, resultData.anotherData.Value3);
            Assert.Equal(100, resultData.Dict["testKey1"]);
            Assert.Equal(101, resultData.Dict["testKey"]);

            workflowInstanceId = StartWorkflow(File.ReadAllText("DictionaryData.yml"), new Dictionary<string, int>());
            WaitForWorkflowToComplete(workflowInstanceId, TimeSpan.FromSeconds(30));
            Dictionary<string, int> resultData2 = GetData<Dictionary<string, int>>(workflowInstanceId);
            Assert.Equal(10, resultData2["Value1"]);
            Assert.Equal(11, resultData2["Value2"]);
        }
    }
}
