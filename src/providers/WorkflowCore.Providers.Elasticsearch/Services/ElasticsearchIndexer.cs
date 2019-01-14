using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.Search;

namespace WorkflowCore.Providers.Elasticsearch.Services
{
    public class ElasticsearchIndexer : ISearchIndex
    {
        private readonly ConnectionSettings _settings;
        private readonly string _indexName;
        private IElasticClient _client;

        public ElasticsearchIndexer(ConnectionSettings settings, string indexName, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _indexName = indexName;
        }

        public async Task IndexWorkflow(WorkflowInstance workflow)
        {
            if (_client == null)
                throw new InvalidOperationException();

            var denormModel = WorkflowSearchResult.FromWorkflowInstance(workflow);
            await _client.IndexAsync(denormModel, x => x.Index(_indexName));
        }

        public async Task<Page<WorkflowSearchResult>> Search(string terms, int skip, int take)
        {
            if (_client == null)
                throw new InvalidOperationException();

            var filters = new List<Func<QueryContainerDescriptor<WorkflowSearchResult>, QueryContainer>>();

            filters.Add(filter => filter.Match(t => t.Field(f => f.Status).Query("Runnable")));

            var dt = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            filters.Add(filter => filter.DateRange(t => t.Field(f => f.CreateTime).GreaterThanOrEquals(dt)));

            var result = await _client.SearchAsync<WorkflowSearchResult>(s => s
                .Index(_indexName)
                .Skip(skip)
                .Take(take)          
                .MinScore(0.1)
                .Query(query => query
                    .Bool(b => b
                        .Filter(filters)
                        .Should(
                            should => should.Match(t => t.Field(f => f.Reference).Query(terms).Boost(1.2)),
                            should => should.Match(t => t.Field(f => f.DataTokens).Query(terms).Boost(1.1)),
                            should => should.Match(t => t.Field(f => f.WorkflowDefinitionId).Query(terms).Boost(0.9)),
                            should => should.Match(t => t.Field(f => f.Status).Query(terms).Boost(0.9)),
                            should => should.Match(t => t.Field(f => f.Description).Query(terms))
                        )
                    )
                )
            );

            return new Page<WorkflowSearchResult>
            {
                Total = result.Total,
                Data = result.Hits.Select(x => x.Source).ToList()
            };
        }

        public Task Start()
        {
            _client = new ElasticClient(_settings);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _client = null;
            return Task.CompletedTask;
        }
    }
}
