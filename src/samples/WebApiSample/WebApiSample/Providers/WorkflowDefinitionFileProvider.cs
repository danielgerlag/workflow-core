using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebApiSample.Providers
{
    public class WorkflowDefinitionFileProvider : IDefinitionProvider
    {
        private const string DefaultTemplateSearchPattern = "*.yml";

        public IEnumerable<string> GetDefinitions()
        {
            return LoadDefinitionFromDirectory();
        }

        private IEnumerable<string> LoadDefinitionFromDirectory()
        {
            var directory = new DirectoryInfo("./WorkflowDSL");

            foreach (var file in directory.GetFiles(DefaultTemplateSearchPattern, SearchOption.AllDirectories)
                                .OrderBy(f => f.FullName))
            {
                var name = Path.GetRelativePath(directory.FullName, file.FullName);
                var rawContent = string.Empty;

                try
                {
                    using var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var streamReader = new StreamReader(stream);
                    rawContent = streamReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Fail to load template file {file.FullName}.", ex);
                }

                if (!string.IsNullOrEmpty(rawContent))
                {
                    yield return rawContent;
                }
            }
        }
    }
}
