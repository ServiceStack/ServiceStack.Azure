using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;

namespace ServiceStack.Azure.Storage
{
    public class AzureBlobVirtualPathProvider : AbstractVirtualPathProviderBase, IVirtualFiles
    {



        public CloudStorageAccount StorageAccount { get; private set; }
        public CloudBlobContainer Container { get; private set; }

        CloudBlobClient client = null;

        private readonly AzureBlobVirtualDirectory rootDirectory;

        public override IVirtualDirectory RootDirectory
        {
            get
            {
                return rootDirectory;
            }
        }

        public override string VirtualPathSeparator
        {
            get { return "/"; }
        }

        public override string RealPathSeparator
        {
            get { return "/"; }
        }


        public AzureBlobVirtualPathProvider(CloudStorageAccount storageAccount, string containerName, IAppHost appHost)
            : base(appHost)
        {
            this.StorageAccount = storageAccount;
            this.client = storageAccount.CreateCloudBlobClient();
            this.Container = client.GetContainerReference(containerName);
            this.Container.CreateIfNotExists();

            this.rootDirectory = new AzureBlobVirtualDirectory(this, null);
        }

        protected override void Initialize()
        {
        }

        public void WriteFile(string filePath, string textContents)
        {
            CloudBlockBlob blob = Container.GetBlockBlobReference(SanitizePath(filePath));
            blob.UploadText(textContents);
        }

        public void WriteFile(string filePath, Stream stream)
        {
            CloudBlockBlob blob = Container.GetBlockBlobReference(SanitizePath(filePath));
            blob.UploadFromStream(stream);
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void DeleteFile(string filePath)
        {
            CloudBlockBlob blob = Container.GetBlockBlobReference(SanitizePath(filePath));
            blob.Delete();
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            filePaths.Each(q => DeleteFile(q));
        }

        public void DeleteFolder(string dirPath)
        {
            dirPath = SanitizePath(dirPath);
            // Delete based on a wildcard search of the directory
            if (!dirPath.EndsWith("/")) dirPath += "/";
            dirPath += "*";
            foreach (var blob in Container.ListBlobs(dirPath, true))
            {
                Container.GetBlockBlobReference(((CloudBlockBlob)blob).Name).DeleteIfExists();
            }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            var filePath = SanitizePath(virtualPath);

            CloudBlockBlob blob = Container.GetBlockBlobReference(filePath);
            if (!blob.Exists()) return null;

            return new AzureBlobVirtualFile(this, GetDirectory(GetDirPath(virtualPath))).Init(blob);
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            return new AzureBlobVirtualDirectory(this, virtualPath);
        }

        public override bool DirectoryExists(string virtualPath)
        {
            var ret = ((AzureBlobVirtualDirectory)GetDirectory(virtualPath)).Exists();
            return ret;
        }

        public string GetDirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var lastDirPos = filePath.LastIndexOf(VirtualPathSeparator[0]);
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public IEnumerable<AzureBlobVirtualFile> GetImmediateFiles(string fromDirPath)
        {
            var dir = new AzureBlobVirtualDirectory(this, fromDirPath);

            return Container.ListBlobs((fromDirPath == null) ? null : fromDirPath + this.RealPathSeparator)
                .Where(q => q.GetType() == typeof(CloudBlockBlob))
                .Select(q => new AzureBlobVirtualFile(this, dir).Init(q as CloudBlockBlob));

        }

        public string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == VirtualPathSeparator[0] ? filePath.Substring(1) : filePath);

            return sanitizedPath != null
                ? sanitizedPath.Replace('\\', VirtualPathSeparator[0])
                : null;
        }
    }
}
