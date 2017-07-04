using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Xunit;

namespace WorkflowCore.Tests.SqlServer
{    
    public class DockerSetup : IDisposable
    {
        //public static string ImageName = "microsoft/mssql-server-windows-express";
        public static string ImageName = "microsoft/mssql-server-linux";
        public static int Port = 1533;
        public static string SqlPassword = "I@mJustT3st1ing";
        DockerClient docker;
        string containerId;

        public string ConnectionString { get; private set; }

        public DockerSetup()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                docker = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            else
                docker = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

            HostConfig hostCfg = new HostConfig();
            PortBinding pb = new PortBinding();
            pb.HostIP = "0.0.0.0";
            pb.HostPort = Port.ToString();
            hostCfg.PortBindings = new Dictionary<string, IList<PortBinding>>();
            hostCfg.PortBindings.Add("1433/tcp", new PortBinding[] { pb });

            var env = new List<string>();
            env.Add("ACCEPT_EULA=Y");
            env.Add($"SA_PASSWORD={SqlPassword}");

            docker.Images.PullImageAsync(new ImagesPullParameters() { Parent = ImageName, Tag = "latest" }, null).Wait();
            //docker.Images.CreateImageAsync(new ImagesCreateParameters() { Parent = ImageName, Tag = "latest", }, null).Wait();

            var container = docker.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = $"{ImageName}:latest",
                Name = "workflowcore-sqlserver-tests",
                HostConfig = hostCfg,
                Env = env
            }).Result;


            bool started = docker.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()).Result;
            if (started)
            {
                containerId = container.ID;
                Console.Write("Waiting 10 seconds for SQL Server to start in the docker container...");
                Thread.Sleep(15000); //allow time for SQL to start
                Console.WriteLine("10 seconds are up.");
                //var ct = new CancellationToken();
                //var waitResult = docker.Containers.WaitContainerAsync(container.ID, ct).Result;


                Console.WriteLine("Docker container started: " + containerId);
                ConnectionString = $"Server=127.0.0.1,{Port};Database=workflowcore-tests;User Id=sa;Password={SqlPassword};";
            }
            else
            {
                Console.WriteLine("Docker container failed");
            }
        }

        public void Dispose()
        {
            docker.Containers.KillContainerAsync(containerId, new ContainerKillParameters()).Wait();
            docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters() { Force = true }).Wait();
        }        
    }

    [CollectionDefinition("SqlServer collection")]
    public class SqlServerCollection : ICollectionFixture<DockerSetup>
    {        
    }

}