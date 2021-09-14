using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Docker.Testify;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{
    public class SqlDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }

        public override string ImageName => "mcr.microsoft.com/mssql/server";
        public override int InternalPort => 1433;
        public override TimeSpan TimeOut => TimeSpan.FromSeconds(120);

        public const string SqlPassword = "I@mJustT3st1ing";

        public override IList<string> EnvironmentVariables => new List<string> {"ACCEPT_EULA=Y", $"SA_PASSWORD={SqlPassword}"};

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"Server=127.0.0.1,{ExternalPort};Database=workflowcore-tests;User Id=sa;Password={SqlPassword};";
            ScenarioConnectionString = $"Server=127.0.0.1,{ExternalPort};Database=workflowcore-scenario-tests;User Id=sa;Password={SqlPassword};";
        }

        public override bool TestReady()
        {
            try
            {
                var client = new SqlConnection($"Server=127.0.0.1,{ExternalPort};Database=master;User Id=sa;Password={SqlPassword};");
                client.Open();
                client.Close();
                return true;
            }
            catch
            {
                return false;
            }

        }
    }

    [CollectionDefinition("SqlServer collection")]
    public class SqlServerCollection : ICollectionFixture<SqlDockerSetup>
    {        
    }

}