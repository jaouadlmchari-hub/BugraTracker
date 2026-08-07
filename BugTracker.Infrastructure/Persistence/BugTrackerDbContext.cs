using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence
{
    public class BugTrackerDbContext : DbContext
    {
        public BugTrackerDbContext( DbContextOptions<BugTrackerDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Project> Projects => Set<Project>();

        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

        public DbSet<Sprint> Sprints => Set<Sprint>();

        public DbSet<Epic> Epics => Set<Epic>();

        public DbSet<Issue> Issues => Set<Issue>();

        public DbSet<Comment> Comments => Set<Comment>();

        public DbSet<Attachment> Attachments => Set<Attachment>();

        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(BugTrackerDbContext).Assembly);
        }

    }
}
