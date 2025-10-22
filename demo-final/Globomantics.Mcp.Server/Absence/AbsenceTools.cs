using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Globomantics.Mcp.Server.Calendar;
using Globomantics.Mcp.Server.Documents;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Absence;

[McpServerToolType]
public static class AbsenceTools
{
    [McpServerTool, Description("Help an employee plan when and whether they can take vacation, leave, or personal days based on their eligibility, benefit plan documentation, and the company calendar")]
    public static async Task<IEnumerable<ContentBlock>> PlanAbsence(
        IHrmAbsenceApi hrmAbsenceApi,
        IHrmDocumentService hrmDocumentService,
        string employeeId,
        CancellationToken cancellationToken)
    {
        var contentBlocks = await PlanAbsenceAsync(hrmAbsenceApi, hrmDocumentService, employeeId, cancellationToken).ToListAsync(cancellationToken);
        return contentBlocks;
    }

    private static async IAsyncEnumerable<ContentBlock> PlanAbsenceAsync(IHrmAbsenceApi hrmAbsenceApi, IHrmDocumentService hrmDocumentService, string employeeId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var employeeDetails = await hrmAbsenceApi.GetWorkerByIdAsync(employeeId, cancellationToken);
        var eligibleAbsenceTypes = await hrmAbsenceApi.GetEligibleAbsenceTypesAsync(employeeId, "not_used", cancellationToken);
    
        yield return new TextContentBlock
        {
            Text = $"Here are the employee's eligible absence types: {JsonSerializer.Serialize(eligibleAbsenceTypes, McpJsonUtilities.DefaultOptions)}"
        };

        var workLocation = employeeDetails.HQLocation switch
        {
            "US" => WorkLocation.UnitedStates,
            "IN" => WorkLocation.India,
            _ => WorkLocation.UnitedStates
        };
        
        foreach (var block in CalendarTools.GetWorkCalendar([DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1], workLocation))
        {
            yield return block;
        }

        var employeeBenefitPlans = await hrmAbsenceApi.GetWorkerBenefitPlansAsync(employeeId, "json", cancellationToken);
        var benefitPlanDocuments = await hrmDocumentService.GetBenefitPlanDocumentsAsync(cancellationToken);
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
}