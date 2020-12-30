using Raven.Client.Documents.Operations;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Models;
using Raven.Client;
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;
using WorkflowCore.Persistence.RavenDB.Services;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.RavenDB;

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
				return new RavendbPersistenceProvider(store);
			});

			options.Services.AddTransient<IWorkflowPurger>(sp =>
			{
				return new WorkflowPurger(store);
			});

			return options;
		}
	}
}