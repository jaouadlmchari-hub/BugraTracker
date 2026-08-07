using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        
        builder.ToTable("activity_logs");

       
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
               .HasDefaultValueSql("NEWID()");

        
        builder.Property(a => a.Action)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(a => a.Field)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(a => a.FromValue)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(a => a.ToValue)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(a => a.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

       
        builder.HasIndex(a => a.IssueId);

        builder.HasOne(a => a.Issue)
               .WithMany(i => i.ActivityLogs)
               .HasForeignKey(a => a.IssueId)
               .OnDelete(DeleteBehavior.Cascade);

      
        builder.HasOne(a => a.User)
               .WithMany(u => u.ActivityLogs)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}