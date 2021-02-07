﻿using System;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowCore.Interface
{
    public interface IDefinitionLoader
    {
        WorkflowDefinition LoadDefinition(string source, Func<string, DefinitionSourceV1> deserializer);
        Task<WorkflowDefinition> LoadDefinitionAsync(string source, Func<string, DefinitionSourceV1> deserializer);
    }
}