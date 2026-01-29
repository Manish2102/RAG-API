using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using DataAccessLayer.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories
{
    public class DocumentUploadRepository : IDocumentUploadInterface
    {
        private readonly string _connectionString;

        private readonly string _containerName;
        public DocumentUploadRepository(IConfiguration configuration )
        {
            _connectionString = configuration["AzureBlob:url"] ?? throw new ArgumentException("azure blob connection string required");
            _containerName = configuration["AzureBlob:containerName"] ?? "customgpt-data";
        }

        public async Task<Stream> DownloadDocumentAsync(string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;
            return stream;
        }


        public async Task<string> UploadDocumentAsync(Stream fileStream, string fileName)
        {
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            return blobClient.Uri.ToString();

        }
    }
}
