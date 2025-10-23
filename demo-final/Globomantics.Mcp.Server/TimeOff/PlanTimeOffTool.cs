using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Globomantics.Mcp.Server.Calendar;
using Globomantics.Mcp.Server.Documents;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class PlanTimeOffTool
{
    private readonly IHrmAbsenceApi hrmAbsenceApi;
    private readonly IHrmDocumentService hrmDocumentService;
    private readonly SearchClient searchClient;

    public PlanTimeOffTool(IHrmAbsenceApi hrmAbsenceApi,
        IHrmDocumentService hrmDocumentService,
        SearchClient searchClient)
    {
        this.hrmAbsenceApi = hrmAbsenceApi;
        this.hrmDocumentService = hrmDocumentService;
        this.searchClient = searchClient;
    }
    
    [McpServerTool, Description("Help an employee plan when and whether they can take vacation, leave, or personal days based on their eligibility, benefit plan documentation, and the company calendar")]
    public async Task<IEnumerable<ContentBlock>> PlanTimeOff(
        string employeeId,
        CancellationToken cancellationToken)
    {
        var contentBlocks = await PlanTimeOffAsync(employeeId, cancellationToken).ToListAsync(cancellationToken);
        return contentBlocks;
    }

    private async IAsyncEnumerable<ContentBlock> PlanTimeOffAsync(string employeeId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var employeeDetails = await hrmAbsenceApi.GetWorkerByIdAsync(employeeId, cancellationToken);

        yield return new TextContentBlock
        {
            Text = $"Employee details: {JsonSerializer.Serialize(employeeDetails, McpJsonUtilities.DefaultOptions)}"
        };

        var eligibleAbsenceTypes = await hrmAbsenceApi.GetEligibleAbsenceTypesAsync(employeeId, "not_used", cancellationToken);

        yield return new TextContentBlock
        {
            Text = $"Eligible absence types: {JsonSerializer.Serialize(eligibleAbsenceTypes, McpJsonUtilities.DefaultOptions)}"
        };

        yield return new TextContentBlock
        {
            Text = "Currently planned time off:"
        };
        yield return new EmbeddedResourceBlock
        {
            Resource = new TextResourceContents()
            {
                Uri = CalendarResources.ResourceEmployeeCalendarUri.Replace("{employeeId}", employeeId),
                MimeType = "application/json",
                Text = await CalendarResources.EmployeeCalendarResource(employeeId, hrmAbsenceApi, cancellationToken),
            }
        };

        await foreach (var block in ProvideRelevantPlanDocumentLinks(employeeId, cancellationToken))
        {
            yield return block;
        }

        await foreach (var block in ProvideRelevantPlanExcerptsAsync(
            queryText: $"time off policies for {string.Join(", ", eligibleAbsenceTypes)}",
            k: 3,
            cancellationToken))
        {
            yield return block;
        }
    }

    private async IAsyncEnumerable<ContentBlock> ProvideRelevantPlanDocumentLinks(string employeeId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var employeeBenefitPlans = await hrmAbsenceApi.GetWorkerBenefitPlansAsync(employeeId, "json", cancellationToken);
        var currentlyEffectivePlans = employeeBenefitPlans.BenefitPlans.Where(p => DateTime.UtcNow >= p.StartDate && p.EndDate >= DateTime.UtcNow).ToList();

        if (currentlyEffectivePlans.Count == 0)
        {
            yield return new TextContentBlock
            {
                Text = $"The employee is not currently enrolled in any benefit plans, so this limits the available absence planning options."
            };
            yield break;
        }

        yield return new TextContentBlock
        {
            Text = $"Here are the relevant benefit plan document resource links the employee is enrolled in that you can refer to directly for official policies:"
        };

        var benefitPlanDocuments = await hrmDocumentService.GetBenefitPlanDocumentsAsync(cancellationToken);
        foreach (var plan in currentlyEffectivePlans)
        {
            var matchingDocument = benefitPlanDocuments.Find(doc => doc.Category?.ToString() == plan.PlanType.Id);
            if (matchingDocument != null)
            {
                yield return new ResourceLinkBlock
                {
                    Uri = DocumentResources.ResourceBenefitPlanDocumentUri.Replace("{documentId}", matchingDocument.DocumentId),
                    MimeType = "application/pdf",
                    Name = matchingDocument.Title,
                };
            }
        }
    }
    
    private async IAsyncEnumerable<ContentBlock> ProvideRelevantPlanExcerptsAsync(string queryText, int k, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new TextContentBlock
        {
            Text = "Here are some relevant time off policy document excerpts that may help answer questions about planning time off work:"
        };

        var searchOptions = new SearchOptions
        {
            Filter = "",
            Size = k,
            Select = { "title", "chunk_id", "chunk" },
            IncludeTotalCount = true,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizableTextQuery(text: queryText)
                    {
                        KNearestNeighborsCount = k,
                        Fields = {"text_vector"},
                        Exhaustive = false
                    }
                }
            }
        };

        var searchResults = await searchClient.SearchAsync<SearchDocument>(null, searchOptions, cancellationToken: cancellationToken);

        if (searchResults.HasValue)
        {
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                yield return new TextContentBlock
                {
                    Text = $"Document ID: {result.Document["title"]}\n---\n{result.Document["chunk"]}"
                };
            }
        }
    }
}