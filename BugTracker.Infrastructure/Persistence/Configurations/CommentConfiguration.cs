using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BugTracker.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        
        builder.ToTable("comments");

      
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
               .HasDefaultValueSql("NEWID()");

        
        builder.Property(c => c.Content)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        builder.Property(c => c.UpdatedAt)
               .IsRequired();

       
        builder.HasOne(c => c.Issue)
               .WithMany(i => i.Comments)
               .HasForeignKey(c => c.IssueId)
               .OnDelete(DeleteBehavior.Cascade);

    
        builder.HasOne(c => c.Author)
               .WithMany(u => u.Comments)
               .HasForeignKey(c => c.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}