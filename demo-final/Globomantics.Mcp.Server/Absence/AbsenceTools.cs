using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Absence;

[McpServerToolType]
public static class AbsenceTools
{
    [McpServerTool, Description("Get the absence eligibility for the current employee such as if they can take vacation, sick leave, or personal days")]
    public static async Task<CallToolResult> GetEmployeeAbsenceEligibility(RequestContext<CallToolRequestParams> context, string employeeId)
    {
        try
        {
            var hrmAbsenceApi = context.Services!.GetService<IHrmAbsenceApi>();
            var eligibleAbsenceTypes = await hrmAbsenceApi!.GetEligibleAbsenceTypesAsync(employeeId, "not_used");
            var result = new CallToolResult()
            {
                Content = [
                new TextContentBlock
                {
                    Text = $"Here are the employee's eligible absence types: {JsonSerializer.Serialize(eligibleAbsenceTypes)}"
                },

            ],
            };

            return result;
        }
        catch (Exception ex)
        {
            return new CallToolResult()
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = $"Error retrieving employee absence eligibility: {ex.Message}"
                    }
                ],
            };
        }


    }
}