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
        private readonly ILogger _logger;
        private IElasticClient _client;
        
        public ElasticsearchIndexer(ConnectionSettings settings, string indexName, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _indexName = indexName;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public async Task IndexWorkflow(WorkflowInstance workflow)
        {
            if (_client == null)
                throw new InvalidOperationException("Not started");

            try
            {
                var denormModel = WorkflowSearchResult.FromWorkflowInstance(workflow);
                await _client.IndexAsync(denormModel, x => x.Index(_indexName));
            }
            catch (Exception ex)
            {
                _logger.LogError(default(EventId), ex, $"Failed to index workflow {workflow.Id}");
            }
        }

        public async Task<Page<WorkflowSearchResult>> Search(string terms, int skip, int take, params SearchFilter[] filters)
        {
            if (_client == null)
                throw new InvalidOperationException("Not started");
            
            var result = await _client.SearchAsync<WorkflowSearchResult>(s => s
                .Index(_indexName)
                .Skip(skip)
                .Take(take)
                .MinScore(!string.IsNullOrEmpty(terms) ? 0.1 : 0)
                .Query(query => query
                    .Bool(b => b
                        .Filter(BuildFilterQuery(filters))
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

        public async Task Start()
        {
            _client = new ElasticClient(_settings);
            var ping = await _client.PingAsync();
        }

        public Task Stop()
        {
            _client = null;
            return Task.CompletedTask;
        }

        private List<Func<QueryContainerDescriptor<WorkflowSearchResult>, QueryContainer>> BuildFilterQuery(SearchFilter[] filters)
        {
            var result = new List<Func<QueryContainerDescriptor<WorkflowSearchResult>, QueryContainer>>();

            foreach (var filter in filters)
            {
                switch (filter)
                {
                    case ScalarFilter f:
                        result.Add(x => x.Match(t => t.Field(f.Property).Query(Convert.ToString(f.Value))));
                        break;
                    case DateRangeFilter f:
                        if (f.BeforeValue.HasValue)
                            result.Add(x => x.DateRange(t => t.Field(f.Property).LessThan(f.BeforeValue)));
                        if (f.AfterValue.HasValue)
                            result.Add(x => x.DateRange(t => t.Field(f.Property).GreaterThan(f.AfterValue)));
                        break;
                    case NumericRangeFilter f:
                        if (f.LessValue.HasValue)
                            result.Add(x => x.Range(t => t.Field(f.Property).LessThan(f.LessValue)));
                        if (f.GreaterValue.HasValue)
                            result.Add(x => x.Range(t => t.Field(f.Property).GreaterThan(f.GreaterValue)));
                        break;
                }
            }

            return result;
        }
    }
}
