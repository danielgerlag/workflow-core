using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Services.DefinitionStorage
{
    public class TypeResolver : ITypeResolver
    {
        public Type FindType(string name)
        {
            return Type.GetType(name, true, true);
        }
    }
}
