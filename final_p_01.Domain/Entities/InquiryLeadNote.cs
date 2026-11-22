using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

public class InquiryLeadNote
{
    public int Id { get; set; }
    public int InquiryLeadId { get; set; }
    public InquiryLead Lead { get; set; } = null!;
    [MaxLength(2000)] public string Text { get; set; } = "";
    [MaxLength(200)] public string Author { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
