using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.Search
{
    public abstract class SearchFilter
    {
    }

    public class ReferenceFilter : SearchFilter
    {
        public string Value { get; set; }

        public static ReferenceFilter Equals(string value) => new ReferenceFilter()
        {
            Value = value
        };
    }

    public class StatusFilter : SearchFilter
    {
        public WorkflowStatus Value { get; set; }

        public static StatusFilter Equals(WorkflowStatus value) => new StatusFilter()
        {
            Value = value
        };
    }

    public class WorkflowDefinitionFilter : SearchFilter
    {
        public string Value { get; set; }

        public static WorkflowDefinitionFilter Equals(string value) => new WorkflowDefinitionFilter()
        {
            Value = value
        };
    }

    public class CreateDateFilter : SearchFilter
    {
        public DateTime? BeforeValue { get; set; }
        public DateTime? AfterValue { get; set; }

        public static CreateDateFilter Before(DateTime value) => new CreateDateFilter()
        {
            BeforeValue = value
        };

        public static CreateDateFilter After(DateTime value) => new CreateDateFilter()
        {
            AfterValue = value
        };

        public static CreateDateFilter Between(DateTime start, DateTime end) => new CreateDateFilter()
        {
            BeforeValue = end,
            AfterValue = start
        };
    }

    public class CompleteDateFilter : SearchFilter
    {
        public DateTime? BeforeValue { get; set; }
        public DateTime? AfterValue { get; set; }

        public static CompleteDateFilter Before(DateTime value) => new CompleteDateFilter()
        {
            BeforeValue = value
        };

        public static CompleteDateFilter After(DateTime value) => new CompleteDateFilter()
        {
            AfterValue = value
        };

        public static CompleteDateFilter Between(DateTime start, DateTime end) => new CompleteDateFilter()
        {
            BeforeValue = end,
            AfterValue = start
        };
    }
}
