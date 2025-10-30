using System.ComponentModel;
using System.Text.Json;
using Globomantics.Mcp.Server.Calendar;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class GetTimeOffTool
{
    private readonly IHrmAbsenceApi hrmAbsenceApi;

    public GetTimeOffTool(IHrmAbsenceApi hrmAbsenceApi)
    {
        this.hrmAbsenceApi = hrmAbsenceApi;
    }

    [McpServerTool, Description(
        """
        Get the employee's combined work and personal absence calendars.
        Prompt for their employee ID unless they provided it already.
        This will help you avoid scheduling time off during company holidays or overlapping with existing time off.
        This can also help answer questions about upcoming Globomantics holidays for the current year.
        """)]
    public async Task<IEnumerable<ContentBlock>> GetTimeOff(
        [Description("Provided by the user")] string employeeId,
        CancellationToken cancellationToken)
    {
        var employeeCalendarResource = await CalendarResources.EmployeeCalendarResource(employeeId, hrmAbsenceApi, cancellationToken);
        var employeeDetails = await hrmAbsenceApi.GetWorkerByIdAsync(employeeId, cancellationToken);

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
            }
        ];
    }
}