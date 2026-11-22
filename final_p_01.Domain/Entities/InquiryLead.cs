using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;


public class InquiryLead
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string LeadType { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>شماره موبایل نرمال‌شده به فرم 09xxxxxxxxx</summary>
    [Required, MaxLength(15)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; set; }

    public int? CourtCount { get; set; }

    public byte? CourtType { get; set; }

    [MaxLength(100)]
    public string? MaterialTier { get; set; }

    public long? EstimatedMinCost { get; set; }
    public long? EstimatedMaxCost { get; set; }

    public int? BookingSlotId { get; set; }
    public BookingSlot? BookingSlot { get; set; }

    [MaxLength(100)]
    public string? ServiceType { get; set; }

    [MaxLength(50)]
    public string? VisitType { get; set; }

    public string? Message { get; set; }

    /// <summary>توکن امن برای دانلودهای gated مثل نمونه طرح توجیهی</summary>
    [MaxLength(64)]
    public string? AccessToken { get; set; }

    [MaxLength(100)] public string? SiteType { get; set; }
    [MaxLength(100)] public string? EstimatedStartTime { get; set; }
    [MaxLength(150)] public string? ApproximateBudget { get; set; }
    [MaxLength(50)] public string? Source { get; set; }
    public int? ProjectId { get; set; }
    public Darabico.Domain.Entities.Project? Project { get; set; }
    public final_p_01.Domain.Enums.LeadStatus Status { get; set; }
    [MaxLength(150)] public string? AssignedTo { get; set; }
    [MaxLength(2048)] public string? LandingPage { get; set; }
    [MaxLength(2048)] public string? Referrer { get; set; }
    [MaxLength(200)] public string? UtmSource { get; set; }
    [MaxLength(200)] public string? UtmMedium { get; set; }
    [MaxLength(200)] public string? UtmCampaign { get; set; }
    public List<InquiryLeadAttachment> Attachments { get; set; } = [];
    public List<InquiryLeadNote> Notes { get; set; } = [];
    public bool IsHandled { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
