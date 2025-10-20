using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Documents;

[McpServerResourceType]
public static class DocumentResources
{
    public const string ResourceBenefitPlanDocumentsUri = "globomantics://hrm/benefit-documents";
    public const string ResourceBenefitPlanDocumentUri = "globomantics://hrm/benefit-documents/{documentId}";

    [McpServerResource(UriTemplate = ResourceBenefitPlanDocumentsUri, Name = "Benefit Plan Document List", MimeType = "application/json")]
    [Description("Retrieves a list of available HRM benefit plan documents")]
    public static async Task<string> DocumentListResource(RequestContext<ReadResourceRequestParams> requestContext)
    {
        var blobServiceClient = requestContext.Services?.GetService<BlobServiceClient>() ?? throw new InvalidOperationException("No Azure Blob Service Client was found");
        var containerClient = blobServiceClient.GetBlobContainerClient("globomanticshrm");
        var documentBlobs = await containerClient.GetBlobsAsync(traits: Azure.Storage.Blobs.Models.BlobTraits.Metadata).ToListAsync();
        var documentNames = documentBlobs.Select(b => new
        {
            DocumentId = b.Name,
            Description = b.Metadata.TryGetValue("Description", out string? value) ? value : null
        }).ToList();

        return JsonSerializer.Serialize(documentNames);
    }

    [McpServerResource(UriTemplate = ResourceBenefitPlanDocumentUri, Name = "Benefit Plan Document", MimeType = "text/plain")]
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
