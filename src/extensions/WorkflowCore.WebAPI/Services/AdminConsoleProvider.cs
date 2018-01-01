using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WorkflowCore.WebAPI.Services
{
    public class AdminConsoleProvider : IFileProvider
    {
        private EmbeddedFileProvider _provider;

        public AdminConsoleProvider() 
        {
            _provider = new EmbeddedFileProvider(typeof(AdminConsoleProvider).GetTypeInfo().Assembly, "WorkflowCore.WebAPI.admin_console");
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _provider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return _provider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return _provider.Watch(filter);
        }
    }
}
