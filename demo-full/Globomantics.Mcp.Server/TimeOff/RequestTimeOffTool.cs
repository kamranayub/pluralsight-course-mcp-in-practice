using System.ComponentModel;
using System.Threading.Tasks;
using Globomantics.Hrm.Api;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.TimeOff;

[McpServerToolType]
public class RequestTimeOffTool(IHrmAbsenceApi hrmAbsenceApi)
{
    private readonly IHrmAbsenceApi hrmAbsenceApi = hrmAbsenceApi;

    [McpServerTool(UseStructuredContent = true, Title = "Request Time Off"),
    Description($"""
    Use this tool to request time off for an employee.
    Provide the employee ID, time off type, and the days requested.
    Acceptable time off types are: {nameof(TimeOffRequestType.Vacation)}, {nameof(TimeOffRequestType.PersonalHoliday)}, {nameof(TimeOffRequestType.SickDay)}, {nameof(TimeOffRequestType.MedicalOrFMLALeave)}, {nameof(TimeOffRequestType.PersonalLeaveOfAbsence)}, {nameof(TimeOffRequestType.Sabbatical)}.
    This method can only support one type of time off per request, but you can submit multiple requests with different types if needed.
    Days do not need to be consecutive but should not fall on weekends (Sat/Sun).
    You can prompt the employee to attach their work calendar or planned time off to check if any dates overlap with scheduled company holidays or existing time off and warn them.
    Remind the employee if they are requesting a Personal Holiday that there is a limit to how many they can take per year per company policy.
    Before calling, make sure to summarize the dates and requested time off type clearly.
    """)]
    public async Task<TimeOffResponse> RequestTimeOff(SimpleTimeOffRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employeeIdResponse = await hrmAbsenceApi.GetAuthenticatedUserIdAsync(cancellationToken);
            var employeeId = employeeIdResponse.EmployeeId;
            var eligibleAbsenceTypes = await hrmAbsenceApi.GetEligibleAbsenceTypesAsync(employeeId, "not_used", cancellationToken);
            var absenceRequest = BuildTimeOffRequestFromSimpleRequest(request, eligibleAbsenceTypes.AbsenceTypes);
            var response = await hrmAbsenceApi.RequestTimeOffAsync(employeeId, absenceRequest, cancellationToken);
            return response;
        }
        catch (RestEase.ApiException apiException)
        {
            if (apiException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new McpException(
                    "Failed to request time off. Did the employee provide the correct ID?", apiException);
            }

            throw new McpException(
                $"Failed to request time off. The response was: {apiException.Content}", apiException);
        }
        catch (Exception ex)
        {
            throw new McpException("An unexpected error occurred while requesting time off.", ex);
        }
    }

    private TimeOffRequest BuildTimeOffRequestFromSimpleRequest(SimpleTimeOffRequest request, List<AbsenceType> eligibleAbsenceTypes)
    {

        var absenceType = eligibleAbsenceTypes.FirstOrDefault(at =>
            at.Name == request.TimeOffType.ToAbsenceTypeCode());

        if (absenceType == null)
        {
            throw new InvalidOperationException("No matching absence type found. Get worker details to find eligible absence types.");
        }

        var timeOffDays = request.Days.Select(day =>
            new TimeOffDay(
                day.DayType == TimeOffDayType.HalfDayMorning ? "08:00" : "12:00",
                day.Date.ToString("yyyy-MM-dd"),
                day.DayType == TimeOffDayType.HalfDayMorning ? "12:00" : "17:00",
                day.DayType == TimeOffDayType.FullDay ? 1.0 : 0.5,
                absenceType
            )
        ).ToList();

        return new TimeOffRequest(
            timeOffDays
        );
    }
}

public enum TimeOffDayType
{
    FullDay,
    HalfDayMorning,
    HalfDayAfternoon
}

public record SimpleTimeOffRequest(
    SimpleTimeOffDay[] Days,
    TimeOffRequestType TimeOffType = TimeOffRequestType.Vacation);

public record SimpleTimeOffDay(
    DateTime Date,
    TimeOffDayType DayType);

public record TimeOffResponseError(string error);