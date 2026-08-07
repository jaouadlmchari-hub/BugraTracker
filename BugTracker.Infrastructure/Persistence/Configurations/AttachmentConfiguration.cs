using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
               .HasDefaultValueSql("NEWID()");


        builder.Property(a => a.Filename)
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(a => a.StorageKey)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(a => a.MimeType)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(a => a.SizeBytes)
               .HasColumnName("size_bytes")
               .IsRequired();

        builder.Property(a => a.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

      
        builder.ToTable(t => t.HasCheckConstraint("CK_Attachments_SizeBytes", "[size_bytes] > 0"));

        
        builder.HasIndex(a => a.StorageKey)
               .IsUnique();

        builder.HasOne(a => a.Issue)
               .WithMany(i => i.Attachments)
               .HasForeignKey(a => a.IssueId)
               .OnDelete(DeleteBehavior.Cascade);

     
        builder.HasOne(a => a.Uploader)
               .WithMany()
               .HasForeignKey(a => a.UploaderId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}