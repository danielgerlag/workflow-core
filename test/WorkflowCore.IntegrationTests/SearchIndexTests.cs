using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Equivalency;
using FluentAssertions.Common;
using Xunit;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.Search;

namespace WorkflowCore.IntegrationTests
{
    public abstract class SearchIndexTests
    {
        protected abstract ISearchIndex CreateService();
        protected ISearchIndex Subject { get; set; }

        protected SearchIndexTests()
        {
            Subject = CreateService();
            Subject.Start().Wait();

            foreach (var item in BuildTestData())
                Subject.IndexWorkflow(item).Wait();
            System.Threading.Thread.Sleep(1000);
        }

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
                    Value3 = 7
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
                    Value3 = 5,
                    Value1 = "quick fox",
                    Value2 = "lazy dog" 
                }
            });

            result.Add(new WorkflowInstance()
            {
                Id = "4",
                CreateTime = new DateTime(2010, 1, 1),
                Status = WorkflowStatus.Complete,
                Reference = "ref4",
                Data = new AltDataObject()
                {
                    Value1 = 9,
                    Value2 = new DateTime(2000, 1, 1)
                }
            });

            return result;
        }


        [Fact]
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
        
        [Fact]
        public async void should_search_on_custom_data()
        {
            var result = await Subject.Search("dog fox", 0, 10);

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().NotContain(x => x.Id == "2");
            result.Data.Should().Contain(x => x.Id == "3");
        }

        [Fact]
        public async void should_filter_on_custom_data()
        {
            var result = await Subject.Search(null, 0, 10, ScalarFilter.Equals<DataObject>(x => x.Value3, 7));

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().Contain(x => x.Id == "2");
            result.Data.Should().NotContain(x => x.Id == "3");
        }

        [Fact]
        public async void should_filter_on_alt_custom_data_with_conflicting_names()
        {
            var result1 = await Subject.Search(null, 0, 10, ScalarFilter.Equals<AltDataObject>(x => x.Value1, 9));
            var result2 = await Subject.Search(null, 0, 10, DateRangeFilter.Between<AltDataObject>(x => x.Value2, new DateTime(1999, 12, 31), new DateTime(2000, 1, 2)));

            result1.Data.Should().Contain(x => x.Id == "4");
            result2.Data.Should().Contain(x => x.Id == "4");
        }

        [Fact]
        public async void should_filter_on_reference()
        {
            var result = await Subject.Search(null, 0, 10, ScalarFilter.Equals(x => x.Reference, "ref2"));

            result.Data.Should().NotContain(x => x.Id == "1");
            result.Data.Should().Contain(x => x.Id == "2");
            result.Data.Should().NotContain(x => x.Id == "3");
        }

        [Fact]
        public async void should_filter_on_status()
        {
            var result = await Subject.Search(null, 0, 10, StatusFilter.Equals(WorkflowStatus.Runnable));

            result.Data.Should().NotContain(x => x.Status != WorkflowStatus.Runnable);
            result.Data.Should().Contain(x => x.Status == WorkflowStatus.Runnable);
        }

        [Fact]
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
            public string Value1 { get; set; }
            public string Value2 { get; set; }

            public int Value3 { get; set; }

            public IEnumerable<string> GetSearchTokens()
            {
                return new List<string>()
                {
                    Value1,
                    Value2
                };    
            }
        }

        class AltDataObject
        {
            public int Value1 { get; set; }
            public DateTime Value2 { get; set; }
        }
    }
}
