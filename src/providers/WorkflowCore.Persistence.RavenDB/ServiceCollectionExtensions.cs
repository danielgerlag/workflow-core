using System;
using WorkflowCore.Models;
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;
using WorkflowCore.Persistence.RavenDB.Services;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.RavenDB;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static WorkflowOptions UseRavenDB(this WorkflowOptions options, RavenStoreOptions configOptions)
		{
			IDocumentStore store = new DocumentStore
			{
				Urls = new[] { configOptions.ServerUrl },
				Database = configOptions.DatabaseName,
				Certificate = new X509Certificate2(configOptions.CertificatePath, configOptions.CertificatePassword)
			}.Initialize();

			options.UsePersistence(sp =>
			{
				var loggerFactory = sp.GetService<ILoggerFactory>();
                return new RavendbPersistenceProvider(store, loggerFactory);
			});

			options.Services.AddTransient<IWorkflowPurger>(sp =>
			{
				return new WorkflowPurger(store);
			});

			return options;
		}
	}
}