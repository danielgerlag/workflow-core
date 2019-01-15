using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.IntegrationTests
{
    public abstract class SearchIndexTests
    {
        protected abstract ISearchIndex CreateService();
        protected ISearchIndex Subject { get; set; }

        protected IEnumerable<WorkflowInstance> BuildTestData()
        {
            var result = new List<WorkflowInstance>();

            result.Add(new WorkflowInstance()
            {
                Id = "1",
                CreateTime = new DateTime(2000, 1, 1),
                Status = WorkflowStatus.Runnable,
                Reference = "ref1"
            });

            result.Add(new WorkflowInstance()
            {
                Id = "2",
                CreateTime = new DateTime(2000, 1, 1),
                Status = WorkflowStatus.Runnable,
                Reference = "ref2",
                Data = new DataObject()
                {
                    IntValue = 7
                }
            });

            result.Add(new WorkflowInstance()
            {
                Id = "3",
                CreateTime = new DateTime(2000, 1, 1),
                Status = WorkflowStatus.Complete,
                Reference = "ref3",
                Data = new DataObject()
                {
                    IntValue = 5,
                    StrValue1 = "quick fox",
                    StrValue2 = "lazy dog" 
                }
            });

            return result;
        }

        [OneTimeSetUp]
        public async void Setup()
        {
            Subject = CreateService();
            await Subject.Start();

            foreach (var item in BuildTestData())
                await Subject.IndexWorkflow(item);
        }

        [Test]
        public async void should_search_on_reference()
        {
            var result1 = await Subject.Search("ref1", 0, 10);
            var result2 = await Subject.Search("ref2", 0, 10);

            
        }

        class DataObject : ISearchable
        {
            public string StrValue1 { get; set; }
            public string StrValue2 { get; set; }

            public int IntValue { get; set; }

            public IEnumerable<string> GetSearchTokens()
            {
                return new List<string>()
                {
                    StrValue1,
                    StrValue2
                };    
            }
        }
    }
}
