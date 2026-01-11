using System.Text;
using Azure.Storage.Blobs;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Globomantics.Mcp.Server.Documents;

public interface IHrmDocumentService
{
    Task<List<DocumentInfo>> GetBenefitPlanDocumentsAsync(CancellationToken cancellationToken);

    Task<string?> GetBenefitPlanDocumentContentAsync(string documentId, CancellationToken cancellationToken);

    Task<string> GetBenefitPlanDocumentContentAsPlainTextAsync(string documentId, CancellationToken cancellationToken);
}

public class HrmDocumentService(BlobServiceClient client) : IHrmDocumentService
{
    private readonly BlobServiceClient blobServiceClient = client;
    private readonly string containerName = Environment.GetEnvironmentVariable("HRM_BLOB_SERVICE_BLOBCONTAINERNAME")!;

    public async Task<List<DocumentInfo>> GetBenefitPlanDocumentsAsync(CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var documentBlobs = new List<Azure.Storage.Blobs.Models.BlobItem>();
        await foreach (var blob in containerClient.GetBlobsAsync(traits: Azure.Storage.Blobs.Models.BlobTraits.Metadata, cancellationToken: cancellationToken))
        {
            documentBlobs.Add(blob);
        }
        
        var documentInfos = documentBlobs.Select(b => {
            PlanDocumentCategory? parsedCategory = null;
            if (b.Metadata.TryGetValue("Category", out string? category) && Enum.TryParse(category, out PlanDocumentCategory tempCategory))
            {
                parsedCategory = tempCategory;
            }
            
            return new DocumentInfo(b.Name,
                Path.GetFileNameWithoutExtension(b.Name),
                b.Metadata.TryGetValue("Description", out string? value) ? value : null,
                parsedCategory
            );
        }).ToList();

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

    public async Task<string> GetBenefitPlanDocumentContentAsPlainTextAsync(string documentId, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(documentId);

        var exists = await blobClient.ExistsAsync(cancellationToken);

        if (exists.Value == false)
        {
            return "";
        }

        var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
        using var stream = downloadResult.Value.Content.ToStream();
        using PdfDocument document = PdfDocument.Open(stream);

        var pdfText = new StringBuilder();
        foreach (Page page in document.GetPages())
        {
            string text = ContentOrderTextExtractor.GetText(page);
            IEnumerable<Word> words = page.GetWords(NearestNeighbourWordExtractor.Instance);

            foreach (var word in words)
            {
                pdfText.Append(word.Text);
                pdfText.Append(' ');
            }
        }
        
        return pdfText.ToString();
    }
}

public record DocumentInfo(
    string DocumentId,
    string Title,
    string? Description,
    PlanDocumentCategory? Category);

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