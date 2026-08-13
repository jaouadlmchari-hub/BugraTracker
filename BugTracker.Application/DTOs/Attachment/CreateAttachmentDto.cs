namespace BugTracker.Application.DTOs.Attachments;

public class CreateAttachmentDto
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public Stream FileContent { get; set; } = Stream.Null;
}