using System;
using System.Linq;

namespace WorkflowCore.Interface
{
    public interface ITypeResolver
    {
        Type FindType(string name);
    }
}