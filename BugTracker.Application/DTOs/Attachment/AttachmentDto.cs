public class AttachmentDto
{
    public Guid Id { get; set; }

    public Guid IssueId { get; set; }

    public Guid UploaderId { get; set; }

    public string Filename { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DownloadUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}