using System.ComponentModel.DataAnnotations;
using Darabico.Domain.Enums;

namespace Darabico.Web.Areas.Admin.ViewModels;

public class AdminProjectListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int CourtCount { get; set; }
    public CourtType CourtType { get; set; }
    public int ExecutionYear { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "عنوان پروژه")]
    [Required(ErrorMessage = "عنوان پروژه الزامی است.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "نامک آدرس (Slug)")]
    [MaxLength(220)]
    public string? Slug { get; set; }

    [Display(Name = "نام شهر")]
    [Required(ErrorMessage = "نام شهر الزامی است.")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "تعداد زمین پدل")]
    [Required(ErrorMessage = "تعداد زمین الزامی است.")]
    [Range(1, 500, ErrorMessage = "تعداد زمین باید بین ۱ تا ۵۰۰ باشد.")]
    public int CourtCount { get; set; } = 1;

    [Display(Name = "نوع زمین")]
    [Required(ErrorMessage = "نوع زمین الزامی است.")]
    public CourtType CourtType { get; set; } = CourtType.Outdoor;

    [Display(Name = "سال اجرا (شمسی)")]
    [Required(ErrorMessage = "سال اجرا الزامی است.")]
    [Range(1380, 1500, ErrorMessage = "سال شمسی معتبر وارد کنید.")]
    public int ExecutionYear { get; set; }

    [Display(Name = "مدت زمان اجرا (ماه)")]
    [Required(ErrorMessage = "مدت زمان اجرا الزامی است.")]
    [Range(1, 120, ErrorMessage = "مدت اجرا باید حداقل ۱ ماه باشد.")]
    public int ExecutionDurationMonths { get; set; } = 1;

    [Display(Name = "توضیحات کامل پروژه")]
    [Required(ErrorMessage = "توضیحات پروژه الزامی است.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "تصویر شاخص")]
    public IFormFile? FeaturedImageFile { get; set; }
    public string? ExistingFeaturedImageUrl { get; set; }
    public string? ExistingThumbnailUrl { get; set; }

    [Display(Name = "تصویر قبل از اجرا (اختیاری)")]
    public IFormFile? BeforeImageFile { get; set; }
    public string? ExistingBeforeImageUrl { get; set; }
    public bool RemoveBeforeImage { get; set; }

    [Display(Name = "تصویر بعد از اجرا (اختیاری)")]
    public IFormFile? AfterImageFile { get; set; }
    public string? ExistingAfterImageUrl { get; set; }
    public bool RemoveAfterImage { get; set; }

    [Display(Name = "فایل ویدیو MP4 (اختیاری)")]
    public IFormFile? VideoFile { get; set; }

    [Display(Name = "یا لینک مستقیم ویدیو (اختیاری)")]
    [MaxLength(500)]
    public string? VideoUrl { get; set; }
    public bool RemoveVideo { get; set; }

    [Display(Name = "ترتیب نمایش")]
    public int DisplayOrder { get; set; }

    [Display(Name = "منتشر شود")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "عنوان سئو (Meta Title)")]
    [MaxLength(160)]
    public string? MetaTitle { get; set; }

    [Display(Name = "توضیحات متا (Meta Description)")]
    [MaxLength(320)]
    public string? MetaDescription { get; set; }

    [Display(Name = "توضیح عکس شاخص (Alt)")]
    [MaxLength(200)]
    public string? FeaturedImageAlt { get; set; }

    [Display(Name = "فایل صوتی پادکست (اختیاری)")]
    public IFormFile? AudioFile { get; set; }
    public string? ExistingAudioUrl { get; set; }
    public bool RemoveAudio { get; set; }

    [Display(Name = "عنوان باکس دانلود")]
    [MaxLength(150)]
    public string? AttachmentTitle { get; set; }

    [Display(Name = "توضیح کوتاه دانلود (اختیاری)")]
    [MaxLength(500)]
    public string? AttachmentDescription { get; set; }

    [Display(Name = "فایل دانلود (PDF/ZIP)")]
    public IFormFile? AttachmentFile { get; set; }
    public string? ExistingAttachmentUrl { get; set; }
    public bool RemoveAttachment { get; set; }

    public List<FaqInput>? Faqs { get; set; }

    public class FaqInput
    {
        [MaxLength(500)]
        public string? Question { get; set; }

        public string? Answer { get; set; }
    }

    public List<ExistingGalleryImageViewModel> ExistingGalleryImages { get; set; } = [];

    public List<IFormFile>? NewGalleryFiles { get; set; }
    public List<string>? NewGalleryAltTexts { get; set; }
    public List<int>? NewGallerySortOrders { get; set; }
}

public class ExistingGalleryImageViewModel
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن جایگزین (Alt) الزامی است.")]
    [MaxLength(250)]
    public string AltText { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}