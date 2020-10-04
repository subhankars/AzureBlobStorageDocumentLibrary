using AzureBlobStorageDocumentLibrary.models;
using AzureBlobStorageDocumentLibrary.service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeTypes;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AzureBlobStorageDocumentLibrary.Api.Controllers
{
    [ApiController]
    public class DocumentLibraryController : Controller
    {
        readonly IConfiguration Configuration;
        readonly DocumentLibraryService blobHelper;
        readonly string StorageAccountname;
        readonly string AccountKey;
        readonly string ContainerName;
        public DocumentLibraryController(IConfiguration configuration)
        {
            Configuration = configuration;
            StorageAccountname = Configuration["StorageAccount:AccountName"];
            AccountKey = Configuration["StorageAccount:AccountKey"];
            ContainerName = Configuration["StorageAccount:ContainerName"];
            blobHelper = new DocumentLibraryService(StorageAccountname, AccountKey, ContainerName);
        }

        /// <summary>
        /// List all files in a contaier will ther versions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ListAllFilesWithVersion")]
        public async Task<List<FileModel>> GetAllFiles()
        {
            return await DocumentLibraryRESTService.ListAllDocumentWithVersion(StorageAccountname, AccountKey, ContainerName, CancellationToken.None);
        }

        /// <summary>
        /// Downloads specic version of a file
        /// </summary>
        /// <param name="fileName">File to download</param>
        /// <param name="fileVersion">VersionId of the file</param>
        /// <returns></returns>
        [HttpGet]
        [Route("DownloadSecificFileVersion")]
        public async Task<IActionResult> GetFileAsync(string fileName, string fileVersion)
        {
            var contentType = MimeTypeMap.GetMimeType(Path.GetExtension(fileName));
            var fileByte = await blobHelper.DownloadBlobAsync(fileName, fileVersion);
            return File(fileByte, contentType, fileName);
        }

        /// <summary>
        /// Make a specific version to current version
        /// </summary>
        /// <param name="fileName">File name to restore</param>
        /// <param name="versionToRestore">VersionId to make current version</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RestoreSpecificFileVersion")]
        public IActionResult RestoreFile(string fileName, string versionToRestore)
        {
            blobHelper.RestoreFileToSpecificVersion(StorageAccountname, ContainerName, fileName, versionToRestore);
            return Ok(string.Format("File : {0} restored with Version: {1} successfully", fileName, versionToRestore));
        }

        /// <summary>
        /// Uploads file to the blob container
        /// </summary>
        /// <param name="fileSourcePath">Source file path to upload</param>
        /// <returns></returns>
        [HttpPut]
        [Route("UploadFile")]
        public async Task<IActionResult> UploadFile(string fileSourcePath)
        {
            var fileVersion = await blobHelper.UploadBlobAsync(fileSourcePath);
            return Ok(string.Format("File : {0} uploaded successfully with Version Id: {1} ", fileSourcePath, fileVersion));
        }

        [HttpDelete]
        [Route("DeleteSpecificFileVersion")]
        public async Task<IActionResult> DeleteFileVersion(string fileName, string versionToDelete)
        {
            await blobHelper.DeleteSpecificVersion(fileName, versionToDelete);
            return Ok(string.Format("File : {0} with Version: {1} deleted successfully", fileName, versionToDelete));
        }

        /// <summary>
        /// Gets Latest file version
        /// </summary>
        /// <param name="fileName">File Name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetLatestFileVersion")]
        public IActionResult GetLatestVersion(string fileName)
        {
            var version = blobHelper.GetLatestVersion(fileName);
            return Ok(string.Format("Latest version of file {0} is {1}", fileName, version));
        }
    }
}
