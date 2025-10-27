using System.ComponentModel;
using Globomantics.Mcp.Server.Calendar;
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

    [McpServerTool, Description("Get the employee's combined work and personal absence calendars for planning time off work")]
    public async Task<IEnumerable<ContentBlock>> GetTimeOff(CancellationToken cancellationToken)
    {
        var employeeCalendarResource = await CalendarResources.EmployeeCalendarResource(hrmAbsenceApi, cancellationToken);

        return [
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
                    Text = CalendarResources.WorkCalendarResource(),
                }
            },
            new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents()
                {
                    Uri = CalendarResources.ResourceEmployeeCalendarUri,
                    MimeType = "application/json",
                    Text = employeeCalendarResource,
                }
            }
        ];
    }
}