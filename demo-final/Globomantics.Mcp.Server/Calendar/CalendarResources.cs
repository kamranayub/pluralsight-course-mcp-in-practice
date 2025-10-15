using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server.Calendar;

[McpServerResourceType]
public static class CalendarResources
{
    public const string ResourceWorkCalendarUri = "globomantics://hrm/calendar";

    [McpServerResource(UriTemplate = ResourceWorkCalendarUri, Name = "Work Calendar", MimeType = "application/json")]
    [Description("The current year work calendar")]
    public static string WorkCalendarResource()
    {
        var year = DateTime.Now.Year;
        var calendar = AnnualHolidayCalendar.CreateForYear(year);

        return JsonSerializer.Serialize(calendar);
    }
}

public record AnnualHolidayCalendar
{
    public int Year { get; private set; }

    public Dictionary<string, string> Holidays { get; private set; } = new();

    public static AnnualHolidayCalendar CreateForYear(int year)
    {
        var usFederalHolidays = CreateUSFederalHolidays(year);
        var cal = new AnnualHolidayCalendar()
        {
            Year = year,
            Holidays = usFederalHolidays
        };

        return cal;
    }

    private static Dictionary<string, string> CreateUSFederalHolidays(int year)
    {
        var holidays = new Dictionary<string, string>
        {
            { new DateTime(year, 1, 1).ToString("yyyy-MM-dd"), "New Year's Day" },
            { new DateTime(year, 1, 15).ToString("yyyy-MM-dd"), "Martin Luther King Jr. Day" },
            { new DateTime(year, 2, 19).ToString("yyyy-MM-dd"), "Presidents' Day" },
            { new DateTime(year, 5, 28).ToString("yyyy-MM-dd"), "Memorial Day" },
            { new DateTime(year, 6, 19).ToString("yyyy-MM-dd"), "Juneteenth National Independence Day" },
            { new DateTime(year, 7, 4).ToString("yyyy-MM-dd"), "Independence Day" },
            { new DateTime(year, 9, 3).ToString("yyyy-MM-dd"), "Labor Day" },
            { new DateTime(year, 10, 8).ToString("yyyy-MM-dd"), "Indigenous Peoples' Day" },
            { new DateTime(year, 11, 11).ToString("yyyy-MM-dd"), "Veterans Day" },
            { new DateTime(year, 11, 22).ToString("yyyy-MM-dd"), "Thanksgiving Day" },
            { new DateTime(year, 12, 25).ToString("yyyy-MM-dd"), "Christmas Day" }
        };

        return holidays;
    }
}