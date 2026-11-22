using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>اسلات بازدید حضوری/آنلاین؛ رزرو با شمارش اتمیک BookedCount انجام می‌شود.</summary>
public class BookingSlot
{
    public int Id { get; set; }

    /// <summary>تاریخ اسلات (به وقت تهران)</summary>
    public DateOnly Date { get; set; }

    [MaxLength(5)]
    public string StartTime { get; set; } = "09:00";

    [MaxLength(5)]
    public string EndTime { get; set; } = "10:00";

    public int Capacity { get; set; } = 1;

    /// <summary>تعداد رزروهای ثبت‌شده؛ توکن همزمانی برای جلوگیری از اضافه‌رزرو</summary>
    [ConcurrencyCheck]
    public int BookedCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
