using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>تنظیمات سراسری سایت؛ همیشه فقط یک ردیف با Id = 1 وجود دارد.</summary>
public class SiteSettings
{
    [MaxLength(200)] public string InquiryThanksTitle { get; set; } = "درخواست شما ثبت شد";
    [MaxLength(2000)] public string InquiryThanksBody { get; set; } = "کارشناسان دارابیکو برای بررسی پروژه با شما تماس می‌گیرند.";
    [MaxLength(4000)] public string InquiryCities { get; set; } = "";
    [MaxLength(4000)] public string InquiryBudgetRanges { get; set; } = "";
    [MaxLength(254)] public string? LeadNotificationEmail { get; set; }
    [MaxLength(11)] public string? LeadNotificationPhone { get; set; }

    public const int SingletonId = 1;

    public int Id { get; set; }

    /// <summary>هاست کانونی بدون www یا با www، مثلا darabico.com</summary>
    [MaxLength(200)]
    public string CanonicalHost { get; set; } = "darabico.com";

    public bool CanonicalUseWww { get; set; }

    /// <summary>آدرس ویدیوی کارگاه (فایل mp4 محلی یا URL امبد مثل آپارات)</summary>
    [MaxLength(500)]
    public string? WorkshopVideoUrl { get; set; }

    [MaxLength(500)]
    public string? WorkshopVideoPosterUrl { get; set; }

    [MaxLength(200)]
    public string? WorkshopVideoTitle { get; set; }

    [MaxLength(1000)]
    public string? WorkshopVideoDescription { get; set; }

    /// <summary>مسیر فایل PDF نمونه طرح توجیهی</summary>
    [MaxLength(500)]
    public string? FeasibilityPdfPath { get; set; }

    [MaxLength(200)]
    public string FeasibilityPageTitle { get; set; } = "نمونه طرح توجیهی احداث زمین پدل";

    public string? FeasibilityPageIntro { get; set; }

    [MaxLength(160)]
    public string? FeasibilityMetaTitle { get; set; }

    [MaxLength(320)]
    public string? FeasibilityMetaDescription { get; set; }

    /// <summary>«شیوه پرداخت» در صفحه فرایند اجرا (HTML ادیت‌شده در پنل)</summary>
    public string? PaymentTermsHtml { get; set; }

    /// <summary>«ضمانت و خدمات پس از اجرا» در صفحه فرایند اجرا</summary>
    public string? WarrantyHtml { get; set; }

    [MaxLength(1000)]
    public string? AfterSalesIntro { get; set; }

    /// <summary>کلید فعال‌سازی زیرساخت چندزبانه (EN/AR)؛ پیش‌فرض خاموش</summary>
    public bool EnableMultiLanguage { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
