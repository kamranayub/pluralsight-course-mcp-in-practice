using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerToolType]
public static class CalendarTools
{
    [McpServerTool, Description("Get the current Globomantics work calendar")]
    public static IEnumerable<ContentBlock> GetWorkCalendar()
    {
        yield return new TextContentBlock
        {
            Text = "You can find the Globomantics work calendar below for planning time off work."
        };

        yield return new EmbeddedResourceBlock
        {
            Resource = new TextResourceContents()
            {
                Uri = CalendarResources.ResourceWorkCalendarUri,
                MimeType = "application/json",
                Text = CalendarResources.WorkCalendarResource()
            }
        };
    }
}