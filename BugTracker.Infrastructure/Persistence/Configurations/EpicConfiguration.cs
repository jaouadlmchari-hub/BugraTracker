using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class EpicConfiguration : IEntityTypeConfiguration<Epic>
{
    public void Configure(EntityTypeBuilder<Epic> builder)
    {
        
        builder.ToTable("epics");

       
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasDefaultValueSql("NEWID()");

       
        builder.Property(e => e.Title)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.Description)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(e => e.ColorCode)
               .HasMaxLength(7)
               .HasDefaultValue("#3B82F6")
               .IsRequired();

        builder.Property(e => e.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(EpicStatus.Active)
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        builder.Property(e => e.UpdatedAt)
               .IsRequired();

        
        builder.HasOne(e => e.Project)
               .WithMany(p => p.Epics)
               .HasForeignKey(e => e.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}