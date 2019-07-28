# Using with ASP.NET Core
## How to configure within an ASP.NET Core application

In your startup class, use the `AddWorkflow` extension method to configure workflow core services, and then register your workflows and start the host when you configure the app.
```c#
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddWorkflow(cfg =>
        {
            cfg.UseMongoDB(@"mongodb://mongo:27017", "workflow");
            cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://elastic:9200")), "workflows");
        });
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseMvc();

        var host = app.ApplicationServices.GetService<IWorkflowHost>();
        host.RegisterWorkflow<TestWorkflow, MyDataClass>();
        host.Start();
    }
}
```

## Usage

Now simply inject the services you require into your controllers
* IWorkflowController
* IWorkflowHost
* ISearchIndex
* IPersistenceProvider