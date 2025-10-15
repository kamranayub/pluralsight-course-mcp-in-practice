using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerToolType]
public static class Tools
{
    [McpServerTool, Description("Find information about the Globomantics work calendar for vacation, absence, and time off planning")]
    public static CallToolResult GetWorkCalendar()
    {
        var result = new CallToolResult()
        {
            Content = [
                new TextContentBlock
                {
                    Text = "You can find the Globomantics work calendar below along with the current employee's absence calendar details for planning time off work."
                },
                new EmbeddedResourceBlock
                {
                    Resource = new TextResourceContents()
                    {
                        Uri = CalendarResources.ResourceWorkCalendarUri,
                        MimeType = "application/json",
                        Text = CalendarResources.WorkCalendarResource()
                    }
                },
                new EmbeddedResourceBlock
                {
                    Resource = new TextResourceContents()
                    {
                        Uri = CalendarResources.ResourceEmployeeCalendarUri,
                        MimeType = "application/json",
                        Text = CalendarResources.EmployeeCalendarResource()
                    }
                }
            ],
        };

        return result;
    }
}