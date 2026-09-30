using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {

        
       
        builder.ToTable("AuditLogs");

       
        builder.HasKey(a => a.Id);

      
        builder.Property(a => a.UserId)
            .IsRequired(false); 

        builder.Property(a => a.UserEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(a => a.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.OldValue)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.NewValue)
            .HasColumnType("nvarchar(max)") 
            .IsRequired(false);

        builder.Property(a => a.Details)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45) 
            .IsRequired(false);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

       
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull); 
      

        
        builder.HasIndex(a => a.CreatedAtUtc)
            .HasDatabaseName("IX_AuditLogs_CreatedAtUtc");

        
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

      
        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

       
        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityName_EntityId");
    }
}