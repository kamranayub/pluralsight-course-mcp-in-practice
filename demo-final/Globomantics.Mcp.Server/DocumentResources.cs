using System.ComponentModel;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server;

[McpServerResourceType]
public static class DocumentResources
{
    [McpServerResource(UriTemplate = "globomantics://hrm/benefit-documents/{documentId}", Name = "Benefit Plan Documents", MimeType = "text/plain")]
    [Description("Retrieves a specific HRM benefit plan document by its resource ID")]
    public static ResourceContents DocumentResourceById(RequestContext<ReadResourceRequestParams> requestContext, string documentId)
    {
        var blobServiceClient = requestContext.Services?.GetService<BlobServiceClient>() ?? throw new InvalidOperationException("No Azure Blob Service Client was found");
        var containerClient = blobServiceClient.GetBlobContainerClient("globomanticshrm");
        var blobClient = containerClient.GetBlobClient(documentId);

        var exists = blobClient.Exists();

        if (exists.Value == false)
        {
            throw new McpException("Benefit plan document resource not found", McpErrorCode.InternalError);
        }

        var downloadResult = blobClient.DownloadContent();

        return new BlobResourceContents
        {
            Blob = Convert.ToBase64String(downloadResult.Value.Content),
            MimeType = "application/pdf",
            Uri = $"globomantics://hrm/benefit-documents/{documentId}",
        };
    }

    public async static ValueTask<CompleteResult> GetCompletionsForDocumentRequest(RequestContext<CompleteRequestParams> requestContext, CancellationToken cancellationToken)
    {
        var result = new CompleteResult()
        {
            Completion = new Completion()
        };

        if (requestContext.Params?.Argument.Name == "documentId")
        {
            var documentSearchValue = requestContext.Params.Argument.Value;
            var blobServiceClient = requestContext.Services?.GetService<BlobServiceClient>() ?? throw new InvalidOperationException("No Azure Blob Service Client was found");
            var containerClient = blobServiceClient.GetBlobContainerClient("globomanticshrm");

            var blobListRequest = containerClient.GetBlobsAsync(cancellationToken: cancellationToken);
            var blobList = await blobListRequest
                .Where(blobItem => blobItem.Name.Contains(documentSearchValue, StringComparison.OrdinalIgnoreCase))
                .Select(blobItem => blobItem.Name)
                .ToListAsync(cancellationToken);

            result.Completion.Values = blobList;
        }

        return result;
    }
}
