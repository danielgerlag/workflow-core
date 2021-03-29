using System;
using System.Collections.Generic;
using Docker.Testify;
using Npgsql;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL
{
    public class PostgresDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }

        public override string ImageName => "postgres";
        public override int InternalPort => 5432;

        private const string PostgresHostAuthMethod = "trust";
        public override IList<string> EnvironmentVariables => new List<string> {
            $"POSTGRES_HOST_AUTH_METHOD={PostgresHostAuthMethod}"
        };
        public override void PublishConnectionInfo()
        {
            ConnectionString = $"Server=127.0.0.1;Port={ExternalPort};Database=workflow;User Id=postgres;";
            ScenarioConnectionString = $"Server=127.0.0.1;Port={ExternalPort};Database=workflow-scenarios;User Id=postgres;";
        }

        public override bool TestReady()
        {
            try
            {
                var connection = new NpgsqlConnection($"Server=127.0.0.1;Port={ExternalPort};Database=postgres;User Id=postgres;");
                connection.Open();
                connection.Close();
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
    
    [CollectionDefinition("Postgres collection")]
    public class PostgresCollection : ICollectionFixture<PostgresDockerSetup>
    {        
    }

}
