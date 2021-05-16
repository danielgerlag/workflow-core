using System;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Models.Search;

namespace WorkflowCore.Interface
{
    public interface ISearchIndex
    {
        Task IndexWorkflow(WorkflowInstance workflow);

        Task<Page<WorkflowSearchResult>> Search(string terms, int skip, int take, params SearchFilter[] filters);

        Task Start();

        Task Stop();
    }
}
