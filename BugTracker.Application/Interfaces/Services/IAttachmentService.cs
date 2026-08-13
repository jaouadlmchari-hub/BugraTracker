using BugTracker.Application.DTOs.Attachments;

public interface IAttachmentService
{
    Task<AttachmentDto> UploadAsync(Guid issueId, CreateAttachmentDto dto);

    Task<string> GetDownloadUrlAsync(Guid attachmentId);

    Task DeleteAsync(Guid attachmentId);
}