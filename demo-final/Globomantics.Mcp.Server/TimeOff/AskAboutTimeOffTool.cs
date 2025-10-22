using System.ComponentModel;
using System.Text.Json;
using Globomantics.Mcp.Server.Documents;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class AskAboutTimeOffTool
{
    private readonly IHrmAbsenceApi hrmAbsenceApi;
    private readonly IHrmDocumentService hrmDocumentService;

    public AskAboutTimeOffTool(IHrmAbsenceApi hrmAbsenceApi, IHrmDocumentService hrmDocumentService)
    {
        this.hrmAbsenceApi = hrmAbsenceApi;
        this.hrmDocumentService = hrmDocumentService;
    }

    [McpServerTool, Description("Answer questions related to the employee's time off and absence policies")]
    public async Task<IEnumerable<ContentBlock>> AskAboutTimeOff(string employeeId, CancellationToken cancellationToken)
    {
        var employeeInfo = await hrmAbsenceApi.GetWorkerByIdAsync(employeeId, cancellationToken);
        var eligibility = await hrmAbsenceApi.GetEligibleAbsenceTypesAsync(employeeId, "not_used", cancellationToken);
        var timeOffPolicyDocumentResource = await DocumentResources.DocumentResourceById(hrmDocumentService, "Globomantics_Vacation_TimeOff_Policy.pdf", cancellationToken);

        return [
            new TextContentBlock
            {
                Text = $"Here are the employee details: {JsonSerializer.Serialize(employeeInfo, McpJsonUtilities.DefaultOptions)}"
            },
            new TextContentBlock
            {
                Text = $"Here are the employee's eligible absence types: {JsonSerializer.Serialize(eligibility, McpJsonUtilities.DefaultOptions)}"
            },
            // TODO: Replace with Azure AI Search to reduce token count
            new EmbeddedResourceBlock
            {
                Resource = timeOffPolicyDocumentResource
            }
        ];
    }
}