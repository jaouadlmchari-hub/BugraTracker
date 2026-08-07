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
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
           
            builder.ToTable("project_members");

            
            builder.HasKey(pm => pm.Id);
            builder.Property(pm => pm.Id)
                   .HasDefaultValueSql("NEWID()");

         
            builder.Property(pm => pm.Role)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(ProjectRole.Developer)
                   .HasSentinel(ProjectRole.Developer)
                   .IsRequired();
                   
            builder.Property(pm => pm.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

         
            builder.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

          
            builder.HasOne(pm => pm.Project)
                   .WithMany(p => p.Members)
                   .HasForeignKey(pm => pm.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.User)
                   .WithMany(u => u.ProjectMemberships)
                   .HasForeignKey(pm => pm.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
