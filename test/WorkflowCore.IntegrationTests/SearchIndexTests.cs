using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Equivalency;
using FluentAssertions.Common;
using NUnit.Framework;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.Search;

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
                CreateTime = new DateTime(2010, 1, 1),
                Status = WorkflowStatus.Runnable,
                Reference = "ref1"
            });

            result.Add(new WorkflowInstance()
            {
                Id = "2",
                CreateTime = new DateTime(2020, 1, 1),
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
                CreateTime = new DateTime(2010, 1, 1),
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

            result1.Data.Should().Contain(x => x.Id == "1");
            result1.Data.Should().NotContain(x => x.Id == "2");
            result1.Data.Should().NotContain(x => x.Id == "3");

            result2.Data.Should().NotContain(x => x.Id == "1");
            result2.Data.Should().Contain(x => x.Id == "2");
            result2.Data.Should().NotContain(x => x.Id == "3");
        }
        
        [Test]
        public async void should_search_on_custom_data()
        {
            var result = await Subject.Search("dog fox", 0, 10);

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().NotContain(x => x.Id == "2");
            result.Data.Should().Contain(x => x.Id == "3");
        }

        [Test]
        public async void should_filter_on_custom_data()
        {
            var result = await Subject.Search(null, 0, 10, ScalarFilter.Equals<DataObject>(x => x.Data.IntValue, 7));

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().Contain(x => x.Id == "2");
            result.Data.Should().NotContain(x => x.Id == "3");
        }

        [Test]
        public async void should_filter_on_reference()
        {
            var result = await Subject.Search(null, 0, 10, ScalarFilter.Equals(x => x.Reference, "ref2"));

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().Contain(x => x.Id == "2");
            result.Data.Should().NotContain(x => x.Id == "3");
        }

        [Test]
        public async void should_filter_on_status()
        {
            var result = await Subject.Search(null, 0, 10, StatusFilter.Equals(WorkflowStatus.Runnable));

            result.Data.Should().NotContain(x => x.Status != WorkflowStatus.Runnable.ToString());
            result.Data.Should().Contain(x => x.Status == WorkflowStatus.Runnable.ToString());
        }

        [Test]
        public async void should_filter_on_date_range()
        {
            var start = new DateTime(2000, 1, 1);
            var end = new DateTime(2015, 1, 1);
            var result = await Subject.Search(null, 0, 10, DateRangeFilter.Between(x => x.CreateTime, start, end));

            result.Data.Should().NotContain(x => x.CreateTime < start || x.CreateTime > end);
            result.Data.Should().Contain(x => x.CreateTime > start && x.CreateTime < end);
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
