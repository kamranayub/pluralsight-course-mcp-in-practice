using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class CalendarPrompts
{
    [McpServerPrompt(Title = "Next Scheduled Work Holiday", Name = "Next Scheduled Work Holiday")]
    [Description("Finds the next scheduled work holiday for where you work.")]
    public static IEnumerable<PromptMessage> GetNextScheduledHoliday(
        [Description($"Your work location: {nameof(WorkLocation.UnitedStates)} or {nameof(WorkLocation.India)}")]
        WorkLocation employeeLocation,

        [Description("The work year to use for the calendar. If not provided, the current year is used.")]
        string? workYear = null)
    {
        var year = string.IsNullOrWhiteSpace(workYear) ? DateTime.Now.Year : int.Parse(workYear);

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
                Text = "When is my next scheduled work holiday?"
            }
        };
    }
}