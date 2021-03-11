using System;
using System.Collections.Generic;

namespace WorkflowCore.Interface
{
    public interface ISearchable
    {
        IEnumerable<string> GetSearchTokens();
    }
}
