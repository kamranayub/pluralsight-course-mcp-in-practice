using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class CalendarPrompts
{
    [McpServerPrompt(Title = "Suggest Time Off Work")]
    public static string SuggestTimeOffPrompt()
    {
        return "When is the next scheduled Globomantics work holiday? Please suggest some good dates for planning time off, such as 3- or 4-day weekends.";
    }
}