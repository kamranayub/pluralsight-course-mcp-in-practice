using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class TimeOffPrompts
{
    [McpServerPrompt(Title = "Suggest Time Off Work")]
    public static string SuggestTimeOffPrompt()
    {
        return "Using the Globomantics time off planning tool, please suggest some good dates for my next vacation, such as 3- or 4-day weekends.";
    }
}