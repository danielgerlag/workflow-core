using System;

namespace WorkflowCore.TestAssets.DataTypes
{
    public class CounterBoardWithDynamicData: CounterBoard
    {
        public DynamicData DynamicDataInstance { get; set; }
        public CounterBoard CounterBoardInstance { get; set; }
    }
}
