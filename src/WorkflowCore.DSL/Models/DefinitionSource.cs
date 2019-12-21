using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.DefinitionStorage
{
    public abstract class DefinitionSource
    {
        public string Id { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }
    }
}
