using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

public class InquiryLeadAttachment
{
    public int Id { get; set; }
    public int InquiryLeadId { get; set; }
    public InquiryLead Lead { get; set; } = null!;
    [MaxLength(255)] public string OriginalFileName { get; set; } = "";
    [MaxLength(80)] public string StorageKey { get; set; } = "";
    [MaxLength(50)] public string ContentType { get; set; } = "";
    public long ByteLength { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
