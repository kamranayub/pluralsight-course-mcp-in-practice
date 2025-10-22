using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Globomantics.Mcp.Server.TimeOff;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerResourceType]
public static class CalendarResources
{
    public const string ResourceWorkCalendarUri = "globomantics://hrm/calendars/work";

    [McpServerResource(UriTemplate = ResourceWorkCalendarUri, Name = "Work Calendar", MimeType = "application/json")]
    [Description("The current year work calendar")]
    public static string WorkCalendarResource()
    {
        var usCalendar = AnnualHolidayCalendar.CreateForYear(DateTime.Now.Year, WorkLocation.UnitedStates);
        var inCalendar = AnnualHolidayCalendar.CreateForYear(DateTime.Now.Year, WorkLocation.India);

        return JsonSerializer.Serialize(new { US = usCalendar, IN = inCalendar }, McpJsonUtilities.DefaultOptions);
    }

    public const string ResourceEmployeeCalendarUri = "globomantics://hrm/calendars/employee/{employeeId}";

    [McpServerResource(UriTemplate = ResourceEmployeeCalendarUri, Name = "Employee Calendar", MimeType = "application/json")]
    [Description("The current year employee time-off calendar")]
    public static async Task<string> EmployeeCalendarResource(string employeeId, IHrmAbsenceApi hrmAbsenceApi, CancellationToken cancellationToken)
    {
        var employeeTimeOff = await hrmAbsenceApi.GetWorkerPlannedTimeOffAsync(employeeId, "json", cancellationToken);

        return JsonSerializer.Serialize(employeeTimeOff, McpJsonUtilities.DefaultOptions);
    }

    public const string ResourceWorkByLocationCalendarUri = "globomantics://hrm/calendars/work/{year}/{location}";

    [McpServerResource(UriTemplate = ResourceWorkByLocationCalendarUri, Name = "Work Calendar by Location", MimeType = "application/json")]
    [Description("The work calendar for a specific year and location")]
    public static string WorkCalendarByLocationResource(int year, WorkLocation location)
    {
        var calendar = AnnualHolidayCalendar.CreateForYear(year, location);
        return JsonSerializer.Serialize(calendar, McpJsonUtilities.DefaultOptions);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<WorkLocation>))]
public enum WorkLocation
{
    UnitedStates,
    India
}

public record WorkHoliday(string Day, string Holiday);

public record AnnualHolidayCalendar(int Year, WorkHoliday[] Holidays)
{
    public static AnnualHolidayCalendar CreateForYear(int year, WorkLocation location = WorkLocation.UnitedStates)
    {
        var holidays = location == WorkLocation.UnitedStates ? CreateUSFederalHolidays(year) : CreateIndiaHolidays(year);
        var cal = new AnnualHolidayCalendar(year, holidays);

        return cal;
    }

    private static WorkHoliday[] CreateUSFederalHolidays(int year)
    {
        return [
            new WorkHoliday(new DateTime(year, 1, 1).ToString("yyyy-MM-dd"), "New Year's Day"),
            new WorkHoliday(new DateTime(year, 1, 15).ToString("yyyy-MM-dd"), "Martin Luther King Jr. Day"),
            new WorkHoliday(new DateTime(year, 2, 19).ToString("yyyy-MM-dd"), "Presidents' Day"),
            new WorkHoliday(new DateTime(year, 5, 28).ToString("yyyy-MM-dd"), "Memorial Day"),
            new WorkHoliday(new DateTime(year, 6, 19).ToString("yyyy-MM-dd"), "Juneteenth National Independence Day"),
            new WorkHoliday(new DateTime(year, 7, 4).ToString("yyyy-MM-dd"), "Independence Day"),
            new WorkHoliday(new DateTime(year, 9, 3).ToString("yyyy-MM-dd"), "Labor Day"),
            new WorkHoliday(new DateTime(year, 10, 8).ToString("yyyy-MM-dd"), "Indigenous Peoples' Day"),
            new WorkHoliday(new DateTime(year, 11, 11).ToString("yyyy-MM-dd"), "Veterans Day"),
            new WorkHoliday(new DateTime(year, 11, 22).ToString("yyyy-MM-dd"), "Thanksgiving Day"),
            new WorkHoliday(new DateTime(year, 12, 25).ToString("yyyy-MM-dd"), "Christmas Day")
        ];
    }

    private static WorkHoliday[] CreateIndiaHolidays(int year)
    {
        return [
            new WorkHoliday(new DateTime(year, 1, 26).ToString("yyyy-MM-dd"), "Republic Day"),
            new WorkHoliday(new DateTime(year, 8, 15).ToString("yyyy-MM-dd"), "Independence Day"),
            new WorkHoliday(new DateTime(year, 4, 18).ToString("yyyy-MM-dd"), "Good Friday"),
            new WorkHoliday(new DateTime(year, 10, 2).ToString("yyyy-MM-dd"), "Gandhi Jayanti"),
            new WorkHoliday(new DateTime(year, 10, 2).ToString("yyyy-MM-dd"), "Dussehra"),
            new WorkHoliday(new DateTime(year, 12, 25).ToString("yyyy-MM-dd"), "Christmas Day"),
            new WorkHoliday(new DateTime(year, 3, 14).ToString("yyyy-MM-dd"), "Holi"),
            new WorkHoliday(new DateTime(year, 8, 9).ToString("yyyy-MM-dd"), "Raksha Bandhan"),
            new WorkHoliday(new DateTime(year, 10, 20).ToString("yyyy-MM-dd"), "Diwali")
        ];
    }
}