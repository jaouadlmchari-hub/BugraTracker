using BugTracker.Application.DTOs.Attachments;

public interface IAttachmentService
{
    Task<AttachmentDto?> GetByIdAsync(Guid attachmentId);

    Task<IEnumerable<AttachmentDto>> GetByIssueAsync(Guid issueId);

    Task<AttachmentDto> UploadAsync(Guid issueId, CreateAttachmentDto dto);

    Task<string> GetDownloadUrlAsync(Guid attachmentId);

    Task DeleteAsync(Guid attachmentId);
}