using Globomantics.Hrm.Api;

namespace Globomantics.Mcp.Server.TimeOff;

public enum TimeOffRequestType
{
    Vacation,
    PersonalHoliday,
    SickDay,
    MedicalOrFMLALeave,
    PersonalLeaveOfAbsence,
    Sabbatical
}


public static class TimeOffRequestTypeExtensions
{
    public static string ToAbsenceTypeCode(this TimeOffRequestType requestType) => requestType switch
    {
        TimeOffRequestType.Vacation => "VACATION",
        TimeOffRequestType.PersonalHoliday => "FLEX_DAY",
        TimeOffRequestType.SickDay => "SICK_LEAVE",
        TimeOffRequestType.MedicalOrFMLALeave => "MEDICAL_LEAVE",
        TimeOffRequestType.PersonalLeaveOfAbsence => "LEAVE_OF_ABSENCE",
        TimeOffRequestType.Sabbatical => "X_00SABBATICAL",
        _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType,
            $"Could not determine the time off type. Acceptable values are: {string.Join(", ", Enum.GetNames<TimeOffRequestType>())}")
    };

    public static TimeOffRequestType ToTimeOffRequestType(this AbsenceType absenceType) => absenceType.Name switch
    {
        "VACATION" => TimeOffRequestType.Vacation,
        "FLEX_DAY" => TimeOffRequestType.PersonalHoliday,
        "SICK_LEAVE" => TimeOffRequestType.SickDay,
        "MEDICAL_LEAVE" => TimeOffRequestType.MedicalOrFMLALeave,
        "LEAVE_OF_ABSENCE" => TimeOffRequestType.PersonalLeaveOfAbsence,
        "X_00SABBATICAL" => TimeOffRequestType.Sabbatical,
        _ => throw new ArgumentOutOfRangeException(nameof(absenceType), absenceType.Name,
            $"Could not determine the time off type. Acceptable values are: {string.Join(", ", Enum.GetNames<TimeOffRequestType>())}")
    };
}