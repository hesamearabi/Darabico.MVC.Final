using System.Globalization;

namespace Darabico.Web.Helpers;

public static class PersianDateHelper
{
    private static readonly PersianCalendar Calendar = new();

    public static string ToPersianDate(this DateTime utcDateTime)
    {
        var tehranZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), tehranZone);

        int year = Calendar.GetYear(localTime);
        int month = Calendar.GetMonth(localTime);
        int day = Calendar.GetDayOfMonth(localTime);

        return $"{year:0000}/{month:00}/{day:00}";
    }

    public static int GetCurrentPersianYear()
    {
        return Calendar.GetYear(DateTime.UtcNow);
    }
}