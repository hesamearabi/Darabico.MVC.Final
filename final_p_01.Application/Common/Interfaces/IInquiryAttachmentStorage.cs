using final_p_01.Domain.Entities;

namespace final_p_01.Application.Common.Interfaces;

public interface IInquiryAttachmentStorage
{
    Task<InquiryLeadAttachment> SaveAsync(UploadFile file, CancellationToken cancellationToken);
    Stream Open(string storageKey);
    void Delete(string storageKey);
}
