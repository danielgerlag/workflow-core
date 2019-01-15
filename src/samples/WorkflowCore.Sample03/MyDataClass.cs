using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample03
{
    public class MyDataClass : ISearchable
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }

        public int Value3 { get; set; }

        public string ValueStr { get; set; }

        public IEnumerable<string> GetSearchTokens()
        {
            yield return ValueStr;
        }
    }
}
