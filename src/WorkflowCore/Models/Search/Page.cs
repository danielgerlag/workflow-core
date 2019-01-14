using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.Search
{
    public class Page<T>
    {
        public ICollection<T> Data { get; set; }
        public long Total { get; set; }
    }
}
