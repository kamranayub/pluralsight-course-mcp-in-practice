using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class CalendarPrompts
{
    [McpServerPrompt(Title = "Suggest Time Off Work (Basic)")]
    public static string SuggestTimeOffPrompt()
    {
        return "When is the next scheduled Globomantics work holiday? Please suggest some good dates for planning time off, such as 3- or 4-day weekends.";
    }

    [McpServerPrompt(Title = "Suggest Time Off Work (Smart)")]
    public static IEnumerable<PromptMessage> SuggestTimeOffPromptSmart()
    {
        yield return new PromptMessage()
        {
            Role = Role.Assistant,
            Content = new TextContentBlock() {
                Text = "You are an expert HR assistant helping employees plan their time off work based on the provided company's holiday calendar. Do not call a tool, just use the embedded calendar below."
            }
        };

        yield return new PromptMessage()
        {
            Role = Role.Assistant,
            Content = new EmbeddedResourceBlock() {
                Resource = new TextResourceContents() {
                    MimeType = "application/json",
                    Uri = CalendarResources.ResourceWorkCalendarUri,
                    Text = CalendarResources.WorkCalendarResource()
                },
            }
        };

        yield return new PromptMessage()
        {
            Role = Role.User,
            Content = new TextContentBlock()
            {
                Text = "When is the next scheduled Globomantics work holiday? Please suggest some good dates for planning time off, such as 3- or 4-day weekends."
            }
        };            
    }
}