using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;

namespace ServiceStack.Azure.Storage
{
    public class AzureBlobVirtualDirectory : AbstractVirtualDirectoryBase
    {
        private readonly AzureBlobVirtualPathProvider pathProvider;

        public AzureBlobVirtualDirectory(AzureBlobVirtualPathProvider pathProvider, string dirPath)
            : base(pathProvider)
        {
            this.pathProvider = pathProvider;
            this.DirPath = dirPath;
        }

        public string DirPath { get; set; }

        static readonly char DirSep = '/';

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get
            {
                return pathProvider.Container.ListBlobs((this.DirPath == null) ? null : this.DirPath + pathProvider.RealPathSeparator)
                      .Where(q => q.GetType() == typeof(CloudBlobDirectory))
                      .Select(q =>
                      {
                          var blobDir = (CloudBlobDirectory)q;
                          return new AzureBlobVirtualDirectory(pathProvider, blobDir.Prefix.Trim(new char[] { '/' }));
                      });
            }
        }

        public override DateTime LastModified
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<IVirtualFile> Files
        {
            get
            {
                return pathProvider.GetImmediateFiles(this.DirPath);
            }
        }

        // Azure Blob storage directories only exist if there are contents beneath them
        public bool Exists()
        {
            var ret = pathProvider.Container.ListBlobs(this.DirPath, false)
                .Where(q => q.GetType() == typeof(CloudBlobDirectory))
                .Any();
            return ret;

        }

        public override string Name
        {
            get { return DirPath != null ? DirPath.SplitOnLast(pathProvider.RealPathSeparator).Last() : null; }
        }

        public override string VirtualPath
        {
            get { return DirPath; }
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            fileName = pathProvider.CombineVirtualPath(this.DirPath, pathProvider.SanitizePath(fileName));
            return pathProvider.GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var dir = (this.DirPath == null) ? null : this.DirPath + pathProvider.RealPathSeparator;

            var ret = pathProvider.Container.ListBlobs(dir)
                      .Where(q => q.GetType() == typeof(CloudBlockBlob))
                      .Where(q =>
                      {
                          var x = ((CloudBlockBlob)q).Name.Glob(globPattern);
                          return x;
                      })
                      .Select(q =>
                      {
                          return new AzureBlobVirtualFile(pathProvider, this).Init(q as CloudBlockBlob);
                      });
            return ret;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return new AzureBlobVirtualDirectory(this.pathProvider, pathProvider.SanitizePath(DirPath.CombineWith(directoryName)));
        }


    }
}
