using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
       
        builder.ToTable("refresh_tokens");

        
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
               .HasDefaultValueSql("NEWID()");

       
        builder.Property(rt => rt.Token)
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
               .IsRequired();

        builder.Property(rt => rt.IsRevoked)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(rt => rt.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        builder.Property(rt => rt.RevokedAt)
               .IsRequired(false);

       
        builder.HasIndex(rt => rt.Token)
               .IsUnique();

      
        builder.HasOne(rt => rt.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}