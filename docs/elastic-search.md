# Elasticsearch plugin for Workflow Core

A search index plugin for Workflow Core backed by Elasticsearch, enabling you to index your workflows and search against the data and state of them.

## Installing

Install the NuGet package "WorkflowCore.Providers.Elasticsearch"

Using Nuget package console
```
PM> Install-Package WorkflowCore.Providers.Elasticsearch
```

Using .NET CLI
```
dotnet add package WorkflowCore.Providers.Elasticsearch
```


## Configuration

Use the `.UseElasticsearch` extension method on `IServiceCollection` when building your service provider

```C#
using Nest;
...
services.AddWorkflow(cfg =>
{
	...
	cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://localhost:9200")), "index_name");
});
```

## Usage

Inject the `ISearchIndex` service into your code and use the `Search` method.

```
Search(string terms, int skip, int take, params SearchFilter[] filters)
```

#### terms

A whitespace separated string of search terms, an empty string will match everything.
This will do a full text search on the following default fields
 * Reference
 * Description
 * Status
 * Workflow Definition

 In addition you can search data within your own custom data object if it implements `ISearchable`

 ```c#
 using WorkflowCore.Interfaces;
 ...
 public class MyData : ISearchable
{
    public string StrValue1 { get; set; }
    public string StrValue2 { get; set; }

    public IEnumerable<string> GetSearchTokens()
    {
        return new List<string>()
        {
            StrValue1,
            StrValue2
        };    
    }
}
 ```

 ##### Examples

 Search all fields for "puppies"
 ```c#
 searchIndex.Search("puppies", 0, 10);
 ```

#### skip & take

Use `skip` and `take` to page your search results.  Where `skip` is the result number to start from and `take` is the page size.

#### filters

You can also supply a list of filters to apply to the search, these can be applied to both the standard fields as well as any field within your custom data objects.
There is no need to implement `ISearchable` on your data object in order to use filters against it.

The following filter types are available
 * ScalarFilter
 * DateRangeFilter
 * NumericRangeFilter
 * StatusFilter

 These exist in the `WorkflowCore.Models.Search` namespace.

 ##### Examples

 Filtering by reference
 ```c#
 using WorkflowCore.Models.Search;
 ...

 searchIndex.Search("", 0, 10, ScalarFilter.Equals(x => x.Reference, "My Reference"));
 ```

 Filtering by workflows started after a date
 ```c#
 searchIndex.Search("", 0, 10, DateRangeFilter.After(x => x.CreateTime, startDate));
 ```

 Filtering by workflows completed within a period
 ```c#
 searchIndex.Search("", 0, 10, DateRangeFilter.Between(x => x.CompleteTime, startDate, endDate));
 ```

 Filtering by workflows in a state
 ```c#
 searchIndex.Search("", 0, 10, StatusFilter.Equals(WorkflowStatus.Complete));
 ```

 Filtering against your own custom data class
 ```c#

 class MyData
 {
	public string Value1 { get; set; }
	public int Value2 { get; set; }
 }

 searchIndex.Search("", 0, 10, ScalarFilter.Equals<MyData>(x => x.Value1, "blue moon"));
 searchIndex.Search("", 0, 10, NumericRangeFilter.LessThan<MyData>(x => x.Value2, 5))
 ```
