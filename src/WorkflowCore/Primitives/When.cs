﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class When : ContainerStepBody
    {
        public object ExpectedOutcome { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var switchOutcome = GetSwitchOutcome(context);

            if (ExpectedOutcome != switchOutcome)
            {
                if (Convert.ToString(ExpectedOutcome) != Convert.ToString(switchOutcome))
                    return ExecutionResult.Next();
            }

            if (context.PersistenceData == null)
                return ExecutionResult.Branch(new List<object>() { null }, new ControlPersistenceData() { ChildrenActive = true });

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            { 
                bool complete = true;
                foreach (var childId in context.ExecutionPointer.Children)
                    complete = complete && IsBranchComplete(context.Workflow.ExecutionPointers, childId);

                if (complete)
                    return ExecutionResult.Next();
                else
                    return ExecutionResult.Persist(context.PersistenceData);
            }

            throw new Exception("Corrupt persistence data");
        }        

        private object GetSwitchOutcome(IStepExecutionContext context)
        {
            var switchPointer = context.Workflow.ExecutionPointers.First(x => x.Children.Contains(context.ExecutionPointer.Id));
            return switchPointer.Outcome;
        }
    }
}
