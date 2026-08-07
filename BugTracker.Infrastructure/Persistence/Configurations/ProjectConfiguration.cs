using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects");

         
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                   .HasDefaultValueSql("NEWID()");

         
            builder.Property(p => p.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(p => p.Key)
                   .HasMaxLength(6)
                   .IsRequired();

            builder.Property(p => p.Description)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);

            builder.Property(p => p.Status)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(ProjectStatus.Active)
                   .IsRequired();

            builder.Property(p => p.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(p => p.UpdatedAt)
                   .IsRequired();

            builder.HasIndex(p => p.Key).IsUnique();

         
            builder.HasOne(p => p.Owner)
                   .WithMany()
                   .HasForeignKey(p => p.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
