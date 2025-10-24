using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class CalendarPrompts
{
    [McpServerPrompt(Title = "Next Scheduled Work Holiday")]
    public static IEnumerable<PromptMessage> GetNextScheduledHoliday(WorkLocation employeeLocation)
    {
        var year = DateTime.Now.Year;

        yield return new PromptMessage()
        {
            Role = Role.Assistant,
            Content = new TextContentBlock()
            {
                Text = "You are an expert HR assistant helping employees understand the office work schedule. Attached is the employee's location holiday calendar. Use this information to answer questions about scheduled holidays."
            }
        };

        yield return new PromptMessage()
        {
            Role = Role.Assistant,
            Content = new EmbeddedResourceBlock()
            {
                Resource = new TextResourceContents()
                {
                    MimeType = "application/json",
                    Uri = CalendarResources.ResourceWorkByLocationCalendarUri.Replace("{year}", year.ToString()).Replace("{location}", employeeLocation.ToString()),
                    Text = CalendarResources.WorkCalendarByLocationResource(year, employeeLocation)
                },
            }
        };

        yield return new PromptMessage()
        {
            Role = Role.User,
            Content = new TextContentBlock()
            {
                Text = "When is the next scheduled Globomantics work holiday?"
            }
        };
    }
}