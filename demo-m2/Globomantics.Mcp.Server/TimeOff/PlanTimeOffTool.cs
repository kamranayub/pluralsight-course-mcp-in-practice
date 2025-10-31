using System.ComponentModel;
using System.Net.Mail;
using System.Text.Json;
using Globomantics.Mcp.Server.Calendar;
using Globomantics.Mcp.Server.Documents;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class PlanTimeOffTool(IHrmAbsenceApi hrmAbsenceApi, IHrmDocumentService hrmDocumentService)
{
    private readonly IHrmAbsenceApi hrmAbsenceApi = hrmAbsenceApi;
    private readonly IHrmDocumentService hrmDocumentService = hrmDocumentService;

    [McpServerTool(Title = "Plan Time Off"),  Description(
        """
        Help Globomantics employees plan and answer questions about time off.
        The tool includes the employee's scheduled time off calendar and the office location calendars.
        Prompt for their employee ID unless they provided it already.
        This will help you avoid scheduling time off during company holidays or overlapping with existing time off.
        This can also help answer questions about upcoming work holidays for the current year.
        You can use this tool to answer questions about time off, absence, and leave policies.
        """)]
    public async Task<IEnumerable<ContentBlock>> PlanTimeOff(
        [Description("Provided by the user")] string employeeId,
        CancellationToken cancellationToken)
    {
        var employeeCalendarResource = await CalendarResources.EmployeeCalendarResource(employeeId, hrmAbsenceApi, cancellationToken);
        var employeeDetails = await hrmAbsenceApi.GetWorkerByIdAsync(employeeId, cancellationToken);
        var eligibility = await hrmAbsenceApi.GetEligibleAbsenceTypesAsync(employeeId, "not_used", cancellationToken);
        // var timeOffPolicyDocumentResource = await DocumentResources.DocumentResourceById("Globomantics_Vacation_TimeOff_Policy.pdf", hrmDocumentService, cancellationToken);

        return [
            new TextContentBlock
            {
                Text = $"Employee details: {JsonSerializer.Serialize(employeeDetails, McpJsonUtilities.DefaultOptions)}"
            },
            new TextContentBlock
            {
                Text = "You can find the Globomantics work and employee calendar(s) below for planning time off work."
            },
            new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents()
                {
                    Uri = CalendarResources.ResourceWorkCalendarUri,
                    MimeType = "application/json",
                    Text = CalendarResources.WorkCalendarsResource(),
                }
            },
            new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents()
                {
                    Uri = CalendarResources.ResourceEmployeeCalendarUri.Replace("{employeeId}", employeeId),
                    MimeType = "application/json",
                    Text = employeeCalendarResource,
                }
            },
            new TextContentBlock
            {
                Text = $"The employee is eligible for the following types of time off: {
                    string.Join(", ", eligibility.AbsenceTypes.Select(at => at.ToTimeOffRequestType()))}"
            },
            new TextContentBlock
            {
                Text = "You can find the Globomantics Time Off Policy document linked below:"
            },
            new ResourceLinkBlock
            {
                    Name = "Globomantics_Vacation_TimeOff_Policy.pdf",
                    Uri = DocumentResources.ResourceBenefitPlanDocumentUri
                        .Replace("{documentId}", "Globomantics_Vacation_TimeOff_Policy.pdf"),
                    MimeType = "application/pdf",                
            },
        ];
    }
}