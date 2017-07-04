using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Docker.Testify
{
	public abstract class DockerSetup : IDisposable
	{		
        public abstract string ImageName { get; }
        public abstract string ContainerName { get; }
        public abstract int ExternalPort { get; }
        public abstract int InternalPort { get; }

        public virtual string ImageTag => "latest";
        public virtual TimeSpan TimeOut => TimeSpan.FromSeconds(30);
        public virtual IList<string> EnvironmentVariables => new List<string>();

        public abstract bool TestReady();
        public abstract void ContainerReady();

		protected DockerClient docker;
		protected string containerId;

		public DockerSetup()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				docker = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
			else
				docker = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

			HostConfig hostCfg = new HostConfig();
			PortBinding pb = new PortBinding();
			pb.HostIP = "0.0.0.0";
			pb.HostPort = ExternalPort.ToString();
			hostCfg.PortBindings = new Dictionary<string, IList<PortBinding>>();
            hostCfg.PortBindings.Add($"{InternalPort}/tcp", new PortBinding[] { pb });

			docker.Images.PullImageAsync(new ImagesPullParameters() { Parent = ImageName, Tag = ImageTag }, null).Wait();
			
			var container = docker.Containers.CreateContainerAsync(new CreateContainerParameters()
			{
                Image = $"{ImageName}:{ImageTag}",
                Name = $"{ContainerName}-{Guid.NewGuid()}",
				HostConfig = hostCfg,
				Env = EnvironmentVariables
			}).Result;

            Console.WriteLine("Starting docker container...");
			bool started = docker.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()).Result;
			if (started)
			{				
				Console.WriteLine("Waiting service to start in the docker container...");

                var counter = 0;
                var ready = false;

                while ((counter < (TimeOut.TotalSeconds)) && (!ready))
                {
                    Thread.Sleep(1000);
                    ready = TestReady();
                }
                if (ready)
                {
                    Console.WriteLine($"Docker container started: {container.ID}");
                    ContainerReady();
                }
                else
                {
                    Console.WriteLine("Docker container timeout waiting for service");
                }
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
}
