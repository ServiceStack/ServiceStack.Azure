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

The Azure Table Cache Client implements ICacheClient 