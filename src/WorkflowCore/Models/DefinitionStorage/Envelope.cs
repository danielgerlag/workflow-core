using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Envelope
    {
        public int Version { get; set; }
        public DefinitionSource Source { get; set; }
    }
}
