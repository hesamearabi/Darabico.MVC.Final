using System.ComponentModel.DataAnnotations;
using final_p_01.Domain.Entities;

namespace final_p_01.Models;

public class InquiryFormViewModel
{
    public static readonly string[] SiteTypes = ["زمین خالی", "سالن سرپوشیده", "پشت بام", "مجموعه ورزشی موجود", "سایر"];
    public static readonly string[] CourtTypes = ["روباز", "سرپوشیده", "هنوز نمیدانم"];
    public static readonly string[] StartTimes = ["تا ۱ ماه", "۱ تا ۳ ماه", "۳ تا ۶ ماه", "فقط در حال بررسی"];
    [Required(ErrorMessage = "شهر را وارد کنید."), StringLength(100), Display(Name = "شهر")]
    public string City { get; set; } = "";
    [Required(ErrorMessage = "نوع محل را انتخاب کنید."), Display(Name = "نوع محل پروژه")]
    public string SiteType { get; set; } = "";
    [Required(ErrorMessage = "تعداد زمین را وارد کنید."), RegularExpression(@"^(?:[1-9]|1[0-9]|20)$", ErrorMessage = "تعداد زمین باید بین ۱ تا ۲۰ باشد."), Display(Name = "تعداد زمین")]
    public string CourtCount { get; set; } = "";
    [Required(ErrorMessage = "نوع زمین را انتخاب کنید."), Display(Name = "نوع زمین")]
    public string CourtType { get; set; } = "";
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید."), StringLength(150), Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "شماره موبایل را وارد کنید."), RegularExpression(@"^09[0-9]{9}$", ErrorMessage = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود."), Display(Name = "شماره موبایل")]
    public string Mobile { get; set; } = "";
    [Required(ErrorMessage = "زمان شروع را انتخاب کنید."), Display(Name = "زمان تقریبی شروع")]
    public string EstimatedStartTime { get; set; } = "";
    [StringLength(150), Display(Name = "بودجه تقریبی (اختیاری)")]
    public string? ApproximateBudget { get; set; }
    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر است."), Display(Name = "توضیحات (اختیاری)")]
    public string? Description { get; set; }
    [Display(Name = "تصویر یا فایل محل پروژه (اختیاری)")]
    public List<IFormFile> Files { get; set; } = [];
    [StringLength(0)] public string? Website { get; set; }
    public string Source { get; set; } = "inquiry";
    public int? ProjectRef { get; set; }
    public SiteSettings Settings { get; set; } = new();
    public static string[] Options(string? value) => (value ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
}

public class InquirySettingsViewModel
{
    [Required, StringLength(200), Display(Name = "عنوان تأیید درخواست")]
    public string ThanksTitle { get; set; } = "";
    [Required, StringLength(2000), Display(Name = "متن تأیید درخواست")]
    public string ThanksBody { get; set; } = "";
    [StringLength(4000), Display(Name = "شهرها (هر شهر در یک خط؛ خالی یعنی ورود آزاد)")]
    public string? Cities { get; set; }
    [StringLength(4000), Display(Name = "بازه‌های بودجه (هر بازه در یک خط)")]
    public string? BudgetRanges { get; set; }
    [EmailAddress, StringLength(254), Display(Name = "ایمیل دریافت اطلاع‌رسانی لید")]
    public string? NotificationEmail { get; set; }
    [RegularExpression(@"^09[0-9]{9}$"), Display(Name = "موبایل دریافت اطلاع‌رسانی لید")]
    public string? NotificationPhone { get; set; }
}
