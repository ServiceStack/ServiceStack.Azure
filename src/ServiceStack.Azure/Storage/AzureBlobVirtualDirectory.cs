using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;

namespace ServiceStack.Azure.Storage
{
    public class AzureBlobVirtualDirectory : AbstractVirtualDirectoryBase
    {
        private readonly AzureBlobVirtualFiles pathProvider;

        public AzureBlobVirtualDirectory(AzureBlobVirtualFiles pathProvider, string directoryPath)
            : base(pathProvider)
        {
            this.pathProvider = pathProvider;
            this.DirectoryPath = directoryPath;

            if (directoryPath == "/" || directoryPath.IsNullOrEmpty())
                return;

            var separatorIndex = directoryPath.LastIndexOf(pathProvider.RealPathSeparator, StringComparison.Ordinal);

            ParentDirectory = new AzureBlobVirtualDirectory(pathProvider,
                separatorIndex == -1 ? string.Empty : directoryPath.Substring(0, separatorIndex));
        }

        public string DirectoryPath { get; set; }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get
            {
                var blobs = pathProvider.Container.ListBlobs(DirectoryPath == null
                    ? null
                    : DirectoryPath + pathProvider.RealPathSeparator);

                return blobs.Where(q => q.GetType() == typeof(CloudBlobDirectory))
                    .Select(q =>
                    {
                        var blobDir = (CloudBlobDirectory)q;
                        return new AzureBlobVirtualDirectory(pathProvider, blobDir.Prefix.Trim(pathProvider.RealPathSeparator[0]));
                    });
            }
        }

        public override DateTime LastModified => throw new NotImplementedException();

        public override IEnumerable<IVirtualFile> Files => pathProvider.GetImmediateFiles(this.DirectoryPath);

        // Azure Blob storage directories only exist if there are contents beneath them
        public bool Exists()
        {
            var ret = pathProvider.Container.ListBlobs(this.DirectoryPath, false)
                .Where(q => q.GetType() == typeof(CloudBlobDirectory))
                .Any();

            return ret;
        }

        public override string Name => DirectoryPath?.SplitOnLast(pathProvider.RealPathSeparator).Last();

        public override string VirtualPath => DirectoryPath;

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            fileName = pathProvider.CombineVirtualPath(this.DirectoryPath, pathProvider.SanitizePath(fileName));
            return pathProvider.GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var dir = (this.DirectoryPath == null) ? null : this.DirectoryPath + pathProvider.RealPathSeparator;

            var ret = pathProvider.Container.ListBlobs(dir)
                .Where(q => q.GetType() == typeof(CloudBlockBlob))
                .Where(q =>
                {
                    var x = ((CloudBlockBlob)q).Name.Glob(globPattern);
                    return x;
                })
                .Select(q => new AzureBlobVirtualFile(pathProvider, this).Init(q as CloudBlockBlob));
            return ret;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return new AzureBlobVirtualDirectory(this.pathProvider, pathProvider.SanitizePath(DirectoryPath.CombineWith(directoryName)));
        }


    }
}