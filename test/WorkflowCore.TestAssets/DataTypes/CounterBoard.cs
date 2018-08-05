﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.TestAssets.DataTypes
{
    public class CounterBoard
    {
        public NestedCounterBoard Counter1 { get; set; } = new NestedCounterBoard();
        public int Counter2 { get; set; }
        public int Counter3 { get; set; }
        public int Counter4 { get; set; }
        public int Counter5 { get; set; }
        public int Counter6 { get; set; }
        public int Counter7 { get; set; }
        public int Counter8 { get; set; }
        public int Counter9 { get; set; }
        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public bool Flag3 { get; set; }
    }

    public class NestedCounterBoard
    {
        public int Counter { get; set; }
    }
}