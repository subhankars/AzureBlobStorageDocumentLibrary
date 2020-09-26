using AzureBlobStorageDocumentLibrary.helper;
using AzureBlobStorageDocumentLibrary.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AzureBlobStorageDocumentLibrary.service
{
    public static class DocumentLibraryRESTService
    {
        /// <summary>
        /// List all the blobs in a Container with all its version
        /// Referred from https://github.com/tamram/storage-dotnet-rest-api-with-auth
        /// </summary>
        /// <param name="storageAccountName"></param>
        /// <param name="storageAccountKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<List<FileModel>> ListAllDocumentWithVersion(string storageAccountName, string storageAccountKey, string containerName, CancellationToken cancellationToken)
        {
            // I intercepted this url from the Azure Portal. include=versions is doing all the magic here
            var uri = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list&include=versions", storageAccountName, containerName);
            Byte[] requestPayload = null;

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri) { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {
                // Add the request headers for x-ms-date and x-ms-version.
                var now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2019-12-12");

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);

                // Send the request.
                var allFiles = new List<FileModel>();
                using (var httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
                {
                    //   parse the XML response for the container names.
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        var xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                        var x = XElement.Parse(xmlString);

                        foreach (XElement container in x.Element("Blobs").Elements("Blob"))
                        {
                            string fileName = container.Element("Name").Value;

                            var model = new FileModel() { FileName = container.Element("Name").Value, FileVersion = container.Element("VersionId").Value };
                            allFiles.Add(model);
                        }
                    }
                }
                return allFiles;
            }
        }
    }
}
