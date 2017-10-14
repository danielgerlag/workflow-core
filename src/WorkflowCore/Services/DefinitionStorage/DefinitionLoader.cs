using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.DefinitionStorage;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowCore.Services.DefinitionStorage
{
    public class DefinitionLoader
    {
        private readonly IWorkflowRegistry _registry;

        public DefinitionLoader(IWorkflowRegistry registry)
        {
            _registry = registry;
        }


        public void LoadDefinition(string json)
        {
            
            //DefinitionSourceV1 x
        }

        private WorkflowDefinition Convert(DefinitionSourceV1 source)
        {
            var result = new WorkflowDefinition();

            result.Id = source.Id;
            result.Version = source.Version;
            result.Steps = new List<WorkflowStep>();
            result.DefaultErrorBehavior = source.DefaultErrorBehavior;
            result.DefaultErrorRetryInterval = source.DefaultErrorRetryInterval;
            result.Description = source.Description;
            //source.DataType
            //result.DataType

            return result;
        }

        //private Type FindType(string name)
        //{
        //    //System.
        //}

    }
}
