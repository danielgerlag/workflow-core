using System;
using System.Collections.Generic;

namespace WorkflowCore.Models.Search
{
    public class Page<T>
    {
        public ICollection<T> Data { get; set; }
        public long Total { get; set; }
    }
}
