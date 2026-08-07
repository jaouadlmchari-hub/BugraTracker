using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Domain.Entities
{
    public class Attachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid IssueId { get; set; }
        public Guid UploaderId { get; set; }

        public string Filename { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty; // Clé de stockage S3 / Azure Blob
        public string MimeType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        public virtual Issue Issue { get; set; } = null!;
        public virtual User Uploader { get; set; } = null!;
    }
}
