using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Documents;

public class DocumentResources
{
    [McpServerResource(
        UriTemplate = "globomantics://hrm/documents",
        Name = "policy-documents.json",
        Title = "HR Benefit Plan and Policy Documents",
        MimeType = "application/json")]
    [Description("Provides a list of policy documents available to employees. Each policy document is a PDF file and may relate to a specific benefit plan that is available to the employee.")]
    public static async Task<IEnumerable<ResourceContents>> DocumentListResource(IHrmDocumentService documentService, CancellationToken cancellationToken)
    {
        var documentInfos = await documentService.GetBenefitPlanDocumentsAsync(cancellationToken);

        return documentInfos.Select(info => new TextResourceContents
        {
            Text = JsonSerializer.Serialize(info, McpJsonUtilities.DefaultOptions),
            MimeType = "application/json",
            Uri = $"globomantics://hrm/documents/{info.DocumentId}",
        });
    }
}