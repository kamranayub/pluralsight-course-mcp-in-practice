using Globomantics.Mcp.Server.TimeOff;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerPromptType]
public static class CalendarPrompts
{
    [McpServerPrompt(Title = "Next Scheduled Work Holiday")]
    public static async Task<IEnumerable<PromptMessage>> GetNextScheduledHoliday(IHrmAbsenceApi hrmAbsenceApi, CancellationToken cancellationToken)
    {

        var employeeIdResult = await hrmAbsenceApi.GetAuthenticatedUserIdAsync(cancellationToken);
        var employeeDetails = await hrmAbsenceApi.GetWorkerByIdAsync(employeeIdResult.EmployeeId, cancellationToken);
        var employeeLocation = employeeDetails.HQLocation == "US" ? WorkLocation.UnitedStates : WorkLocation.India;

        var year = DateTime.Now.Year;

        return [
            new PromptMessage()
            {
                Role = Role.Assistant,
                Content = new TextContentBlock()
                {
                    Text = "You are an expert HR assistant helping employees understand the office work schedule. Attached is the employee's location holiday calendar. Use this information to answer questions about scheduled holidays."
                }
            },

            new PromptMessage()
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
            },

            new PromptMessage()
            {
                Role = Role.User,
                Content = new TextContentBlock()
                {
                    Text = "When is the next scheduled Globomantics work holiday?"
                }
            }
        ];
    }
}