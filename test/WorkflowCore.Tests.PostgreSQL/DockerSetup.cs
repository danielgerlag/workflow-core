using Docker.DotNet;
using Docker.DotNet.Models;
using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Tests.PostgreSQL
{
    public class DockerSetup : IAssemblyContext
    {

        DockerClient docker = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
        string containerId;
        
        void IAssemblyContext.OnAssemblyStart()
        {
            HostConfig hostCfg = new HostConfig();
            PortBinding pb = new PortBinding();
            pb.HostIP = "0.0.0.0";
            pb.HostPort = "5433";
            hostCfg.PortBindings = new Dictionary<string, IList<PortBinding>>();
            hostCfg.PortBindings.Add("5432/tcp", new PortBinding[] { pb });

            List<string> env = new List<string>();
            env.Add("POSTGRES_PASSWORD=mysecretpassword");

            docker.Images.PullImageAsync(new ImagesPullParameters() { Parent = "postgres", Tag = "latest" }, null).Wait();
            var container = docker.Containers.CreateContainerAsync(new CreateContainerParameters() { Image = "postgres:latest", Name = "workflow-postgres-tests", HostConfig = hostCfg, Env = env }).Result;
            bool started = docker.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()).Result;
            if (started)
            {
                containerId = container.ID;
                Console.WriteLine("Docker container started: " + containerId);
            }
            else
            {
                Console.WriteLine("Docker container failed");
            }
        }

        void IAssemblyContext.OnAssemblyComplete()
        {
            docker.Containers.KillContainerAsync(containerId, new ContainerKillParameters()).Wait();
            docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters() { Force = true }).Wait();
        }

    }
}
