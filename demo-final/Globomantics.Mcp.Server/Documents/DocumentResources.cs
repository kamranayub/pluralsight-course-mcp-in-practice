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
    public static async Task<IEnumerable<ResourceContents>> DocumentListResource(IHrmDocumentService documentService, CancellationToken cancellationToken)
    {
        var documentInfos = await documentService.GetBenefitPlanDocumentsAsync(cancellationToken);

        return documentInfos.Select(info => new TextResourceContents
        {
            Text = JsonSerializer.Serialize(info, McpJsonUtilities.DefaultOptions),
            MimeType = "application/json",
            Uri = ResourceBenefitPlanDocumentUri.Replace("{documentId}", info.DocumentId),
        });
    }

    [McpServerResource(UriTemplate = ResourceBenefitPlanDocumentUri, Name = "Benefit Plan Document")]
    [Description("Retrieves a specific HRM benefit plan document by its resource ID")]
    public static async Task<ResourceContents> DocumentResourceById(string documentId, IHrmDocumentService documentService, CancellationToken cancellationToken)
    {
        var downloadResult = await documentService.GetBenefitPlanDocumentContentAsync(documentId, cancellationToken);

        if (string.IsNullOrEmpty(downloadResult))
        {
            throw new McpProtocolException("Benefit plan document content is empty", McpErrorCode.InternalError);
        }

        return new BlobResourceContents
        {
            Blob = downloadResult,
            MimeType = "application/pdf",
            Uri = ResourceBenefitPlanDocumentUri.Replace("{documentId}", documentId),
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
