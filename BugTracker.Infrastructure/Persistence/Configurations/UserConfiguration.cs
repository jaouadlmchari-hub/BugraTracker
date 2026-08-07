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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

          
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                   .HasDefaultValueSql("NEWID()");

            builder.Property(u => u.Email)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(u => u.Username)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.FullName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(u => u.PasswordHash)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(u => u.AvatarUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(u => u.SystemRole)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(SystemRole.Developer)
                   .IsRequired();

            builder.Property(u => u.IsActive)
                   .HasDefaultValue(true)
                   .IsRequired();

            builder.Property(u => u.FailedLoginAttempts)
                   .HasDefaultValue(0)
                   .IsRequired();

            builder.Property(u => u.LockoutUntil)
                   .IsRequired(false);

            builder.Property(u => u.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(u => u.UpdatedAt)
                   .IsRequired();

            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();
        }
    }
}
