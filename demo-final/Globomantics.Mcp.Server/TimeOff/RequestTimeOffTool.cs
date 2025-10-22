using System.ComponentModel;
using Globomantics.Hrm.Api;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class RequestTimeOffTool
{
    public RequestTimeOffTool(IHrmAbsenceApi hrmAbsenceApi)
    {
        this.hrmAbsenceApi = hrmAbsenceApi;
    }

    private readonly IHrmAbsenceApi hrmAbsenceApi;

    [McpServerTool(UseStructuredContent = true), Description("Request time off for an employee")]
    public async Task<TimeOffResponse> RequestTimeOff(string employeeId, TimeOffRequest request, CancellationToken cancellationToken)
    {
        var response = await hrmAbsenceApi.RequestTimeOffAsync(employeeId, request, cancellationToken);
        return response;
    }
}