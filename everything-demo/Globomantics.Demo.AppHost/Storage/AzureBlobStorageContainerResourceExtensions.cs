using Aspire.Hosting.Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Globomantics.Demo.AppHost.Storage;

public static class AzureBlobStorageContainerResourceExtensions
{
    public static async Task UploadDocumentsToStorageAsync(this AzureBlobStorageContainerResource blobContainerResource, TokenCredential azureCredential, ILogger logger, CancellationToken cancellationToken)
    {
        var hrmDocumentConnString = await blobContainerResource.GetConnectionProperty("ConnectionString").GetValueAsync(cancellationToken);
        var hrmDocumentBlobEndpoint = await blobContainerResource
            .GetConnectionProperty("Uri").GetValueAsync(cancellationToken);

        var blobServiceClient = hrmDocumentConnString == null
            ? new BlobServiceClient(new Uri(hrmDocumentBlobEndpoint!), azureCredential)
            : new BlobServiceClient(hrmDocumentConnString);

        // Upload PDFs
        var blobContainerName = await blobContainerResource.GetConnectionProperty("BlobContainerName").GetValueAsync(cancellationToken);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var pdfFiles = Directory.GetFiles("./documents", "*.pdf");

        foreach (var pdfFile in pdfFiles)
        {
            var filename = Path.GetFileName(pdfFile);
            var blobClient = blobContainerClient.GetBlobClient(filename);
            var fileInfo = await blobClient.UploadAsync(File.OpenRead(pdfFile), true, cancellationToken);

            logger.LogDebug("Uploaded blob {Filename} with content hash {ContentHash}", filename, fileInfo.Value.ContentHash);
        }
    }
}