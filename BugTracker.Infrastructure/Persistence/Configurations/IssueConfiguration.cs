using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
       
        builder.ToTable("issues");

      
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
               .HasDefaultValueSql("NEWID()");

        builder.Property(i => i.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(i => i.Description)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(i => i.Type)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(IssueType.Task)
               .HasSentinel(IssueType.Task)
               .IsRequired();

        builder.Property(i => i.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(IssueStatus.Todo)
               .IsRequired();

        builder.Property(i => i.Priority)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(Priority.Medium)
               .HasSentinel(Priority.Medium)
               .IsRequired();

        builder.Property(i => i.StoryPoints)
               .HasColumnName("story_points")
               .IsRequired(false);

        builder.Property(i => i.DueDate)
               .HasColumnType("date")
               .IsRequired(false);

        builder.Property(i => i.DisplayOrder)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(i => i.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        builder.Property(i => i.UpdatedAt)
               .IsRequired();


        builder.ToTable(t => t.HasCheckConstraint("CK_Issues_StoryPoints", "[story_points] IS NULL OR [story_points] > 0"));
        builder.HasOne(i => i.Project)
               .WithMany(p => p.Issues)
               .HasForeignKey(i => i.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        
        builder.HasOne(i => i.Sprint)
               .WithMany(s => s.Issues)
               .HasForeignKey(i => i.SprintId)
               .OnDelete(DeleteBehavior.NoAction);

       
        builder.HasOne(i => i.Reporter)
               .WithMany(u => u.CreatedIssues)
               .HasForeignKey(i => i.ReporterId)
               .OnDelete(DeleteBehavior.Restrict);

       
        builder.HasOne(i => i.Assignee)
               .WithMany(u => u.AssignedIssues)
               .HasForeignKey(i => i.AssigneeId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}