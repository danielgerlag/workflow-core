using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Nest;

using WorkflowCore.Interface;
using WorkflowCore.Services.DefinitionStorage;

using WebApiSample.Steps;
using WebApiSample.Providers;
using WebApiSample.Workflows;

namespace WebApiSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (this.Environment.IsDevelopment())
            {
                services.AddWorkflow();
            }
            else
            {
                services.AddWorkflow(cfg =>
                {
                    cfg.UseMongoDB(@"mongodb://mongo:27017", "workflow");
                    cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://elastic:9200")), "workflows");
                });
            }

            services.AddSingleton<IDefinitionProvider, WorkflowDefinitionFileProvider>();

            services.AddControllers();
            services.AddMvc().AddNewtonsoftJson();

            services.AddWorkflowDSL();

            // Add workflow steps
            services.AddTransient<DSLHelloWorldStep>();
           

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "RunneraaS API", Version = "v1" }));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
                        
            app.UseSwagger();            
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // DSL load
            var loader = app.ApplicationServices.GetService<IDefinitionLoader>();
            var definitionProvider = app.ApplicationServices.GetService<IDefinitionProvider>();
            foreach (var def in definitionProvider.GetDefinitions())
            {
                try
                {
                    loader.LoadDefinition(def, Deserializers.Yaml);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            var host = app.ApplicationServices.GetService<IWorkflowHost>();
            host.RegisterWorkflow<TestWorkflow, MyDataClass>();

            host.Start();
        }
    }
}
