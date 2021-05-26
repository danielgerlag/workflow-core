using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowCore.Tests.YmalDefinition
{
    public class MyDataClass
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }

        public Dictionary<String, int> Dict { get; set; }

        public AnotherDataClass anotherData { get; set; }
    }
    public class AnotherDataClass
    {
        public int Value3 { get; set; }
    }
}
