using System;
using System.Globalization;

namespace final_p_01.Common;

public static class DateTimeExtensions
{
    private static readonly PersianCalendar _pc = new PersianCalendar();

    public static string ToPersianDate(this DateTime date)
    {
        int year = _pc.GetYear(date);
        int month = _pc.GetMonth(date);
        int day = _pc.GetDayOfMonth(date);

        return $"{year:0000}/{month:00}/{day:00}";
    }
}