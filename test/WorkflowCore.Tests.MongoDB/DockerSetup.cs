using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{    
    public class DockerSetup : IDisposable
    {
        public static int Port = 28017;
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
            hostCfg.PortBindings.Add("27017/tcp", new PortBinding[] { pb });

            docker.Images.PullImageAsync(new ImagesPullParameters() { Parent = "mongo", Tag = "latest" }, null).Wait();
            var container = docker.Containers.CreateContainerAsync(new CreateContainerParameters() { Image = "mongo:latest", Name = "workflow-mongo-tests", HostConfig = hostCfg }).Result;
            bool started = docker.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()).Result;
            if (started)
            {
                containerId = container.ID;
                Console.WriteLine("Docker container started: " + containerId);
                ConnectionString = $"mongodb://localhost:{Port}";
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

    [CollectionDefinition("Mongo collection")]
    public class MongoCollection : ICollectionFixture<DockerSetup>
    {        
    }

}
