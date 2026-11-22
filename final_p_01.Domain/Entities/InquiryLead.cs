using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>
/// تک‌جدول لیدهای عمومی سایت. نوع لید در LeadType مشخص می‌شود
/// ("estimator", "feasibility_pdf", "booking", "aftersales").
/// </summary>
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

    public bool IsHandled { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
