using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Interface
{
    public interface ISearchable
    {
        IEnumerable<string> GetSearchTokens();
    }
}
