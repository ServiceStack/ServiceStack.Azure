using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ServiceStack.VirtualPath;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ServiceStack.Azure.Storage
{
    public class AzureBlobVirtualFile : AbstractVirtualFileBase
    {

        private readonly AzureBlobVirtualPathProvider pathProvider;
        private readonly CloudBlobContainer container;

        public CloudBlockBlob Blob { get; private set; }

        public AzureBlobVirtualFile(AzureBlobVirtualPathProvider owningProvider, IVirtualDirectory directory)
            : base(owningProvider, directory)
        {
            this.pathProvider = owningProvider;
            this.container = pathProvider.Container;
        }

        public AzureBlobVirtualFile Init(CloudBlockBlob blob)
        {
            this.Blob = blob;
            return this;
        }

        public override DateTime LastModified => Blob.Properties.LastModified?.UtcDateTime ?? DateTime.MinValue;

        public override long Length => Blob.Properties.Length;

        public override string Name => Blob.Name.Contains(pathProvider.VirtualPathSeparator)
            ? Blob.Name.SplitOnLast(pathProvider.VirtualPathSeparator)[1]
            : Blob.Name;

        public string FilePath => Blob.Name;

        public string ContentType => Blob.Properties.ContentType;

        public override string VirtualPath => FilePath;

        public override Stream OpenRead()
        {
            return Blob.OpenRead();
        }

        public override void Refresh()
        {
            CloudBlockBlob blob = pathProvider.Container.GetBlockBlobReference(Blob.Name);
            if (!blob.Exists()) return;

            Init(blob);
        }
    }
}
