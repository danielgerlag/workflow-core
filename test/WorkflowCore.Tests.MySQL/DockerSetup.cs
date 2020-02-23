using System.Collections.Generic;
using Docker.Testify;
using Xunit;
using MySql.Data.MySqlClient;
using System;

namespace WorkflowCore.Tests.MySQL
{
    public class MysqlDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }
        public static string ScenarioConnectionString { get; set; }
        public static string RootPassword => "rootpwd123";

        public override TimeSpan TimeOut => TimeSpan.FromSeconds(60);
                
        public override string ImageName => "mysql";
        public override IList<string> EnvironmentVariables => new List<string> {
            $"MYSQL_ROOT_PASSWORD={RootPassword}"
        };

        public override int InternalPort => 3306;

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"Server=127.0.0.1;Port={ExternalPort};Database=workflow;User=root;Password={RootPassword};";
            ScenarioConnectionString = $"Server=127.0.0.1;Port={ExternalPort};Database=scenarios;User=root;Password={RootPassword};";
        }

        public override bool TestReady()
        {
            try
            {
                var connection = new MySqlConnection($"host=127.0.0.1;port={ExternalPort};user=root;password={RootPassword};database=mysql;");
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

    [CollectionDefinition("Mysql collection")]
    public class MysqlCollection : ICollectionFixture<MysqlDockerSetup>
    {
    }

}
