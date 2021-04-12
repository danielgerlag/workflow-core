using System;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Envelope
    {
        public int Version { get; set; }
        public DefinitionSource Source { get; set; }
    }
}
