using System.Collections.Generic;

namespace WebApiSample.Providers
{
    public interface IDefinitionProvider
    {
        IEnumerable<string> GetDefinitions();
    }
}
