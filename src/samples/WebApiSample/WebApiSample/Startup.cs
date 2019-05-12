using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Swashbuckle.AspNetCore.Swagger;
using WebApiSample.Workflows;
using WorkflowCore.Interface;

namespace WebApiSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            services.AddWorkflow(cfg =>
            {
                cfg.UseMongoDB(@"mongodb://mongo:27017", "workflow");
                cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://elastic:9200")), "workflows");
            });

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" }));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
                        
            app.UseSwagger();            
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            app.UseMvc();

            var host = app.ApplicationServices.GetService<IWorkflowHost>();
            host.RegisterWorkflow<TestWorkflow, MyDataClass>();
            host.Start();
        }
    }
}
