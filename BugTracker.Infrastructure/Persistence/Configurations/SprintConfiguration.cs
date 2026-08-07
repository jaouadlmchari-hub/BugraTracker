using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        
        builder.ToTable("sprints");

       
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
               .HasDefaultValueSql("NEWID()");

        builder.Property(s => s.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(s => s.Goal)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(s => s.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(SprintStatus.Planning)
               .IsRequired();

        builder.Property(s => s.StartDate)
               .IsRequired(false);

        builder.Property(s => s.EndDate)
               .IsRequired(false);

        builder.Property(s => s.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        builder.Property(s => s.UpdatedAt)
               .IsRequired();

        
        builder.HasOne(s => s.Project)
               .WithMany(p => p.Sprints)
               .HasForeignKey(s => s.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}