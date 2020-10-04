using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureBlobStorageDocumentLibrary.service
{
    public class DocumentLibraryService
    {
        BlobContainerClient BlobContainerClient;
        public DocumentLibraryService(string storageAccountname, string accountKey, string containerName)
        {
            string connectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net", storageAccountname, accountKey);
            BlobContainerClient = new BlobContainerClient(connectionString, containerName);
        }

        /// <summary>
        /// Uploads a file to blob storage
        /// </summary>
        /// <param name="filePath">Disk path of the file</param>
        /// <returns>Version of File</returns>
        public async Task<string> UploadBlobAsync(string filePath)
        {
            var blobName = Path.GetFileName(filePath);
            string uploadedDocVersion = string.Empty;
            using (var ms = new MemoryStream(File.ReadAllBytes(filePath)))
            {
                var blobClient = BlobContainerClient.GetBlockBlobClient(blobName);
                var blob = await blobClient.UploadAsync(ms);
                uploadedDocVersion = blob.Value.VersionId;
            }
            return uploadedDocVersion;
        }

        /// <summary>
        /// Doownloads a specific version of a file
        /// </summary>
        /// <param name="fileToDownload"></param>
        /// <param name="fileVersion"></param>
        /// <returns></returns>
        public async Task<byte[]> DownloadBlobAsync(string fileToDownload, string fileVersion)
        {
            using (var ms = new MemoryStream())
            {
                var blobClient = BlobContainerClient.GetBlockBlobClient(fileToDownload);
                // WithVersion() is the key piece here
                var blob = blobClient.WithVersion(fileVersion);
                await blob.DownloadToAsync(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Restores a specific file version and makes it the current version
        /// </summary>
        /// <param name="storageAccountName">Storage account Name</param>
        /// <param name="containerName">Container Name</param>
        /// <param name="fileName"></param>
        /// <param name="sourceVersion">File version that we want to restore</param>
        public void RestoreFileToSpecificVersion(string storageAccountName, string containerName, string fileName, string sourceVersion)
        {
            var blobClient = BlobContainerClient.GetBlockBlobClient(fileName); // this is pointing to the current version
            //versionid={} is the most important piece here
            var sourceBlobUri = new Uri(
                string.Format("https://{0}.blob.core.windows.net/{1}/{2}?versionid={3}",
                storageAccountName, containerName, fileName, sourceVersion));

            // Since it will copy in the same storage account's container, it's a synchronous process
            // Copy Operation will make the specic version as current version
            // See https://docs.microsoft.com/en-us/rest/api/storageservices/copy-blob-from-url#request-headers
            blobClient.StartCopyFromUri(sourceBlobUri);
        }

        /// <summary>
        /// Deletes specific version of a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        /// <param name="versionToDelete">File version to delete</param>
        /// <returns></returns>
        public async Task DeleteSpecificVersion(string fileName, string versionToDelete)
        {
            // WithVersion() is the key piece here
            var blobClient = BlobContainerClient.GetBlockBlobClient(fileName).WithVersion(versionToDelete);
            await blobClient.DeleteAsync();
        }

        /// <summary>
        /// Gets latest VersionId of a given file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetLatestVersion(string fileName)
        {
            var blobClient = BlobContainerClient.GetBlockBlobClient(fileName);
            var docProperties = blobClient.GetPropertiesAsync();
            return docProperties.Result.Value.VersionId;
        }
      
    }
}
