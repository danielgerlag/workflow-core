
# Using with ASP.NET Core

This sample will use `docker-compose` to fire up instances of MongoDB and Elasticsearch to which the sample application will connect.

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

```c#
public class WorkflowsController : Controller
{
    private readonly IWorkflowController _workflowService;
    private readonly IWorkflowRegistry _registry;
    private readonly IPersistenceProvider _workflowStore;
    private readonly ISearchIndex _searchService;

    public WorkflowsController(IWorkflowController workflowService, ISearchIndex searchService, IWorkflowRegistry registry, IPersistenceProvider workflowStore)
    {
        _workflowService = workflowService;
        _workflowStore = workflowStore;
        _registry = registry;
        _searchService = searchService;
    }

    public Task<bool> Suspend(string id)
    {
        return _workflowService.SuspendWorkflow(id);
    }

    ...
}
```