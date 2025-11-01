using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerPromptType]
public static class TimeOffPrompts
{
    [McpServerPrompt(Title = "Suggest Time Off Work", Name = "Suggest Time Off Work")]
    [Description("Uses the planning tool to find ideal dates for time off work, taking into account company holidays and weekends.")]
    public static string SuggestTimeOffPrompt(
        [Description("Your employee ID (e.g. '4562')")]        
        int employeeId)
    {
        return $"Using the Globomantics time off planning tool, please suggest some good dates for my next vacation, such as 3- or 4-day weekends. My employee ID is {employeeId}.";
    }
}