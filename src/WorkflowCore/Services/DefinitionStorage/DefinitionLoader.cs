using Newtonsoft.Json;
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
    public class DefinitionLoader : IDefinitionLoader
    {
        private readonly IWorkflowRegistry _registry;

        public DefinitionLoader(IWorkflowRegistry registry)
        {
            _registry = registry;
        }


        public void LoadDefinition(string json)
        {
            var source = JsonConvert.DeserializeObject<DefinitionSourceV1>(json);
            var def = new StoredWorkflowDefinition(source);
            _registry.RegisterWorkflow(def);
        }

        //private WorkflowDefinition Convert(DefinitionSourceV1 source)
        //{
        //    var result = new WorkflowDefinition
        //    {
        //        Id = source.Id,
        //        Version = source.Version,
        //        Steps = ConvertSteps(source.Steps),
        //        DefaultErrorBehavior = source.DefaultErrorBehavior,
        //        DefaultErrorRetryInterval = source.DefaultErrorRetryInterval,
        //        Description = source.Description,
        //        DataType = FindType(source.DataType)                
        //    };


        //    return result;
        //}


        //private List<WorkflowStep> ConvertSteps(ICollection<StepSourceV1> source)
        //{
        //    var result = new List<WorkflowStep>();



        //    return result;
        //}

        //private Type FindType(string name)
        //{
        //    throw new NotImplementedException();
        //}

    }
}
