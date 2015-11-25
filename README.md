## ServiceStack.Azure

ServiceStack adapters and bindings for Azure backend services.

## Drop-in Virtual FileSystem backed by Azure Blob Storage

You can now use an Azure Blob Storage Container to serve website content with the drop-in **AzureBlobVirtualPathProvider**.

    public class AppHost : AppHostBase
    {
        public override void Configure(Container container)
        {
            //All Razor Views, Markdown Content, imgs, js, css, etc are served from an Azure Blob Storage container
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            VirtualFiles = new AzureBlobVirtualPathProvider(storageAccount, "websitecontent", this);
        }

        public override List<IVirtualPathProvider> GetVirtualFileSources()
        {
            //Add Azure Blob Container as lowest priority Virtual Path Provider 
            var pathProviders = base.GetVirtualFileSources();
            pathProviders.Add(VirtualFiles);
            return pathProviders;
        }
    }


## Caching support with Azure Table Storage

The AzureTableCacheClient implements [ICacheClientExteded](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Caching/ICacheClientExtended.cs) and  
[IRemoveByPattern](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Caching/IRemoveByPattern.cs) using Azure Table Storage.  This provider 
is currently implemented as "last writer wins", so concurrent writes to a single cache entry are discouraged.

    public class AppHost : AppHostBase
    {
        public override void Configure(Container container)
        {
            string cacheConnStr = "UseDevelopmentStorage=true;";
            container.Register<ICacheClient>(new AzureTableCacheClient(cacheConnStr));
        }
    }