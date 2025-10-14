using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Globomantics.Mcp.Server;

[McpServerResourceType]
public static class StaticResources
{
    [McpServerResource(UriTemplate = "globomantics://hrm/holidays", Name = "Holiday Calendar", MimeType = "text/plain")]
    [Description("The current year holiday calendar")]
    public static string HolidayCalendarResource()
    {
        var year = new DateTime().Year;
        var calendar = HolidayCalendar.CreateForYear(year);

        return JsonSerializer.Serialize(calendar);
    }
    
    private record HolidayCalendar
    {
        public int Year { get; private set; }

        public Dictionary<string, string> Holidays => [];

        public static HolidayCalendar CreateForYear(int year)
        {
            var cal = new HolidayCalendar()
            {
                Year = year
            };

            cal.Holidays.Add(new DateTime(year, 11, 24).ToShortDateString(), "Thanksgiving");

            return cal;
        }
    }
}