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
            try
            {
                return Type.GetType(name, true, true);
            }
            catch (TypeLoadException ex)
            {
                throw new InvalidOperationException($"Could not resolve type '{name}'. Ensure the type exists and the assembly is referenced.", ex);
            }
        }
    }
}
