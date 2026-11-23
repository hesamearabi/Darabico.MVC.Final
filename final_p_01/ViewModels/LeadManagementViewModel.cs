using System.ComponentModel.DataAnnotations;
using System.Globalization;
using final_p_01.Domain.Entities;
using final_p_01.Domain.Enums;

namespace final_p_01.ViewModels;

public class LeadFilter
{
    [Display(Name = "وضعیت")] public LeadStatus? Status { get; set; }
    [StringLength(10), Display(Name = "از تاریخ شمسی (۱۴۰۵/۰۱/۰۱)")] public string? From { get; set; }
    [StringLength(10), Display(Name = "تا تاریخ شمسی (۱۴۰۵/۱۲/۲۹)")] public string? To { get; set; }
    [StringLength(100), Display(Name = "شهر")] public string? City { get; set; }
    [StringLength(50), Display(Name = "منبع")] public string? Source { get; set; }
    [StringLength(40), Display(Name = "نوع لید")] public string? LeadType { get; set; }
    [StringLength(150), Display(Name = "نام یا موبایل")] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
}
public class LeadListViewModel
{
    public LeadFilter Filter { get; set; } = new();
    public List<InquiryLead> Leads { get; set; } = [];
    public int Total { get; set; }
    public int Pages => (Total + 24) / 25;
}
public class LeadUpdateViewModel
{
    [Required, EnumDataType(typeof(LeadStatus)), Display(Name = "وضعیت")] public LeadStatus? Status { get; set; }
    [StringLength(150), Display(Name = "مسئول پیگیری")] public string? AssignedTo { get; set; }
}
public static class LeadPresentation
{
    public static string Status(LeadStatus value) => value switch {
        LeadStatus.New => "جدید", LeadStatus.Contacted => "تماس گرفته شد", LeadStatus.ProposalSent => "پیشنهاد ارسال شد", LeadStatus.Contracted => "قرارداد", LeadStatus.Rejected => "رد شد", _ => "نامشخص" };
    public static string Date(DateTime value)
    {
        var date = DateTime.SpecifyKind(value, DateTimeKind.Utc).AddHours(3.5);
        var pc = new PersianCalendar();
        return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00} {date:HH:mm}";
    }
    public static DateTime ParseDate(string value)
    {
        var parts = final_p_01.Application.Services.DigitNormalizer.Normalize(value).Split('/');
        if (parts.Length != 3) throw new FormatException();
        return new PersianCalendar().ToDateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), 0, 0, 0, 0).AddHours(-3.5);
    }
}
