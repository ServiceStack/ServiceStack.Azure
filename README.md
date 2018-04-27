## ServiceStack.Azure

ServiceStack.Azure package provides support to Azure ServiceBus and Azure Blob Storage. All features are incapsulated in single ServiceStack.Azure package. To install package run from NuGet

    PM> Install-Package ServiceStack.Azure

ServiceStack.Azure includes implementation of the following ServiceStack providers:

- [ServiceBusMqServer](#ServiceBusMqServer) - [MQ Server](http://docs.servicestack.net/messaging) for invoking ServiceStack Services via Azure ServiceBus
- [AzureBlobVirtualFiles](#virtual-filesystem-backed-by-azure-blob-storage) - Virtual file system based on Azure Blob Storage
- [AzureAppendBlobVirtualFiles](#virtual-filesystem-backed-by-azure-blob-storage) - Virtual file system based on Azure Blob Storage for appending scenarios
- [AzureTableCacheClient](#caching-support-with-azure-table-storage) - Cache client over Azure Table Storage


### ServiceBusMqServer

The code to configure and start an ServiceBus MQ Server is similar to other MQ Servers:

```csharp

container.Register<IMessageService>(c => new ServiceBusMqServer(ConnectionString));  //prefetch defaults to 0 (Service Bus default) if not provided
// var prefetchCount = 10;
// container.Register<IMessageService>(c => new ServiceBusMqServer(ConnectionString));

var mqServer = container.Resolve<IMessageService>();

mqServer.RegisterHandler<ServiceDto>(ExecuteMessage, 4);  // 4 is the max concurrent calls (threads)    

mqServer.Start();
```

Where ConnectionString is connection string to Service Bus, how to obtain it from Azure Portal you can find in [Get Started with Service Bus queues](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues) article.

The prefetch account defaults to 0 and can be used to allow the clients to load additional messages from the service when it receives a read operation.  You can find out more from [Best Practices for performance improvements using Service Bus Messaging](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements#prefetching).

The number of thread parameter to the RegisterHandler gets or sets the maximum number of concurrent calls to the callback the message pump should initiate.  

When an MQ Server is registered, ServiceStack automatically publishes Requests accepted on the "One Way" pre-defined route to the registered MQ broker. The message is later picked up and executed by a Message Handler on a background Thread.

## Virtual FileSystem backed by Azure Blob Storage

You can use an Azure Blob Storage Container to serve website content with the **AzureBlobVirtualFiles**.

```csharp
public class AppHost : AppHostBase
{
    public override void Configure(Container container)
    {
        //All Razor Views, Markdown Content, imgs, js, css, etc are served from an Azure Blob Storage container

        //Use connection string to Azure Storage Emulator. For real application you should use connection string
        //to your Azure Storage account
        var azureBlobConnectionString = "UseDevelopmentStorage=true";
        //Azure container which hold your files. If it does not exist it will be automatically created.
        var containerName = "myazurecontainer";

        VirtualFiles = new AzureBlobVirtualFiles(connectionString, containerName);
        AddVirtualFileSources.Add(VirtualFiles);
    }
}
```

In addition you can use **AzureAppendBlobVirtualFiles** in scenarios that require appending such as logging. 

```csharp
public class AppHost : AppHostBase
{
    public override void Configure(Container container)
    {
      Plugins.Add(new RequestLogsFeature
      {
        RequestLogger = new CsvRequestLogger(
        files: new AzureAppendBlobVirtualFiles(AppSettings.Get<string>("storageConnection"), "logfiles"),
        requestLogsPattern: "requestlogs/{year}-{month}/{year}-{month}-{day}.csv",
        errorLogsPattern: "requestlogs/{year}-{month}/{year}-{month}-{day}-errors.csv",
        appendEvery: TimeSpan.FromSeconds(30))
                
       });
    }
}
```

## Caching support with Azure Table Storage

The AzureTableCacheClient implements [ICacheClientExteded](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Caching/ICacheClientExtended.cs) and [IRemoveByPattern](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Caching/IRemoveByPattern.cs) using Azure Table Storage.

```csharp
public class AppHost : AppHostBase
{
    public override void Configure(Container container)
    {
        string cacheConnStr = "UseDevelopmentStorage=true;";
        container.Register<ICacheClient>(new AzureTableCacheClient(cacheConnStr));
    }
}
```
