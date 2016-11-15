using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore
{
    public static class ExtensionMethods
    {
        
        //public static IEnumerable<WorkflowStep> FlattenChildren(this WorkflowStep root)
        //{
        //    var nodes = new Stack<WorkflowStep>(new[] { root });
        //    while (nodes.Any())
        //    {
        //        WorkflowStep node = nodes.Pop();
        //        yield return node;
        //        foreach (var n in node.Outcomes.Select(x => x.NextStep))
        //            nodes.Push(n);
        //    }
        //}

        
    }
}
