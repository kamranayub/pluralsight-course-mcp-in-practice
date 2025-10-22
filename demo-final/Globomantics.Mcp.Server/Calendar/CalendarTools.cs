using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerToolType]
public static class CalendarTools
{
    [McpServerTool, Description("Get the current Globomantics work calendar")]
    public static IEnumerable<ContentBlock> GetWorkCalendar(int[] calendarYears, WorkLocation? workLocation)
    {
        yield return new TextContentBlock
        {
            Text = "You can find the Globomantics work calendar(s) below for planning time off work."
        };

        foreach (var year in calendarYears)
        {
            yield return new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents()
                {
                    Uri = CalendarResources.ResourceWorkByLocationCalendarUri,
                    MimeType = "application/json",
                    Text = CalendarResources.WorkCalendarByLocationResource(year, workLocation ?? WorkLocation.UnitedStates)
                }
            };
        }
    }
}