using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServiceStack.Azure.Storage
{
    public static class CloudBlobContainerExtension
    {
#if NETSTANDARD1_6

        public static IEnumerable<IListBlobItem> ListBlobs(this CloudBlobContainer container, string prefix = null,
            bool useFlatBlobListing = false)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> blobs = new List<IListBlobItem>();
            do
            {
                var blobResults = container.ListBlobsSegmentedAsync(prefix, useFlatBlobListing,
                        BlobListingDetails.None, 100, continuationToken, null, null)
                    .Result;
                continuationToken = blobResults.ContinuationToken;

                blobs.AddRange(blobResults.Results);
            } while (continuationToken != null);

            return blobs;
        }

        public static void CreateIfNotExists(this CloudBlobContainer container)
        {
            container.CreateIfNotExistsAsync().Wait();
        }

        public static void DeleteIfExists(this CloudBlobContainer container)
        {
            container.DeleteIfExistsAsync().Wait();
        }

        public static void Delete(this CloudBlockBlob blob)
        {
            blob.DeleteAsync().Wait();
        }

        public static void DeleteIfExists(this CloudBlockBlob blob)
        {
            blob.DeleteIfExistsAsync().Wait();
        }

        public static void UploadText(this CloudBlockBlob blob, string content)
        {
            blob.UploadTextAsync(content).Wait();
        }

        public static void UploadFromStream(this CloudBlockBlob blob, Stream stream)
        {
            blob.UploadFromStreamAsync(stream).Wait();
        }

        public static Stream OpenRead(this CloudBlockBlob blob)
        {
            return blob.OpenReadAsync().Result;
        }

        public static bool Exists(this CloudBlockBlob blob)
        {
            return blob.ExistsAsync().Result;
        }

        public static TableResult Execute(this CloudTable table, TableOperation op)
        {
            return table.ExecuteAsync(op).Result;
        }

        public static bool CreateIfNotExists(this CloudTable table)
        {
            return table.CreateIfNotExistsAsync().Result;
        }

        public static IEnumerable<TElement> ExecuteQuery<TElement>(this CloudTable table, TableQuery<TElement> query) where TElement : ITableEntity, new()
        {
            TableContinuationToken continuationToken = null;
            var elements = new List<TElement>();

            do
            {
                var result = table.ExecuteQuerySegmentedAsync(query, continuationToken).Result;
                continuationToken = result.ContinuationToken;
                elements.AddRange(result.Results);
            } while (continuationToken != null);

            return elements;
        }


#endif
    }
}
