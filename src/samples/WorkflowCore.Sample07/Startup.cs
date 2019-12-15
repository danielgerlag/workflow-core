using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample07
{
    public class Startup
    {
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
            services.AddMvc();
        }

        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole();

            //start the workflow host
            var host = app.ApplicationServices.GetService<IWorkflowHost>();
            host.RegisterWorkflow<Sample03.PassingDataWorkflow, Sample03.MyDataClass>();
            host.RegisterWorkflow<Sample04.EventSampleWorkflow, Sample04.MyDataClass>();            
            host.Start();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(); 
        }
    }
}
