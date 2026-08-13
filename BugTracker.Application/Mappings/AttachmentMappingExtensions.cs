using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings
{
    public static class AttachmentMappingExtensions
    {
        public static AttachmentDto ToDto(this Attachment attachment, string downloadUrl)
        {
            return new AttachmentDto
            {
                Id = attachment.Id,
                IssueId = attachment.IssueId,
                UploaderId = attachment.UploaderId,
                Filename = attachment.Filename,
                MimeType = attachment.MimeType,
                SizeBytes = attachment.SizeBytes,
                DownloadUrl = downloadUrl,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}
