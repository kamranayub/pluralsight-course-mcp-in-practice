using Azure.Storage.Blobs;

namespace Globomantics.Mcp.Server.Documents;

public interface IHrmDocumentService
{
    Task<List<DocumentInfo>> GetBenefitPlanDocumentsAsync(CancellationToken cancellationToken);

    Task<string?> GetBenefitPlanDocumentContentAsync(string documentId, CancellationToken cancellationToken);
}

public class HrmDocumentService : IHrmDocumentService
{
    private readonly BlobServiceClient blobServiceClient;
    private static readonly string containerName = "globomanticshrm";

    public HrmDocumentService(BlobServiceClient blobServiceClient)
    {
        this.blobServiceClient = blobServiceClient;
    }

    public async Task<List<DocumentInfo>> GetBenefitPlanDocumentsAsync(CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var documentBlobs = await containerClient.GetBlobsAsync(traits: Azure.Storage.Blobs.Models.BlobTraits.Metadata).ToListAsync();
        var documentInfos = documentBlobs.Select(b => new DocumentInfo(b.Name,
            Path.GetFileNameWithoutExtension(b.Name),
            b.Metadata.TryGetValue("Description", out string? value) ? value : null,
            b.Metadata.TryGetValue("Category", out string? category) && Enum.TryParse(category, out PlanDocumentCategory parsedCategory) ? parsedCategory : null
        )).ToList();

        return documentInfos;
    }
    
    public async Task<string?> GetBenefitPlanDocumentContentAsync(string documentId, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(documentId);

        var exists = await blobClient.ExistsAsync(cancellationToken);

        if (exists.Value == false)
        {
            return null;
        }

        var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);

        return Convert.ToBase64String(downloadResult.Value.Content);
    }
}

public record DocumentInfo(string DocumentId, string Title, string? Description, PlanDocumentCategory? Category);

/// <summary>
/// Maps to BenefitPlanType in Globomantics.Hrm.Api as an example
/// of manual mapping you might do in a real service between two separate services.
/// This could also come from a database or storage service.
/// </summary>
public enum PlanDocumentCategory
{
    Absence,
    Medical,
    Dental,
    Vision,
    Retirement
}