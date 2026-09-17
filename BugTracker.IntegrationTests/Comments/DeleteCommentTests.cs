using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Comments
{
    public class DeleteCommentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteCommentTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<ProjectDto> CreateProjectAsync(string key)
        {
            var dto = new CreateProjectDto
            {
                Name = $"Project {key}",
                Key = key
            };

            var response = await Client.PostAsJsonAsync("/api/projects", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Type = IssueType.Task,
                Priority = Priority.Medium
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<IssueDto>())!;
        }

        private async Task<CommentDto> CreateCommentAsync(Guid issueId, string content)
        {
            var dto = new CreateCommentDto
            {
                Content = content
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/issues/{issueId}/comments",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<CommentDto>())!;
        }

        private async Task AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                dbContext.Set<ProjectMember>().Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                });

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task<bool> CommentExistsAsync(Guid commentId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext
                    .Set<BugTracker.Domain.Entities.Comment>()
                    .AnyAsync(c => c.Id == commentId);
            });

            return exists;
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.DeleteAsync(
                $"/api/comments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenCommentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.DeleteAsync(
                $"/api/comments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenUserHasNoDeletePermission_ShouldReturnForbiddenAndKeepComment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CDF");
            var issue = await CreateIssueAsync(project.Id, "Delete Comment Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Protected comment");

            await AuthenticateAsync(
                "comment-delete-developer@test.com",
                "comment-delete-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "comment-delete-developer@test.com",
                ProjectRole.Developer);

            var response = await Client.DeleteAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var exists = await CommentExistsAsync(comment.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenUserIsAuthor_ShouldReturnNoContentAndDeleteComment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CDA");
            var issue = await CreateIssueAsync(project.Id, "Author Delete Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Author comment");

            var response = await Client.DeleteAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await CommentExistsAsync(comment.Id);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_WhenUserIsProjectManager_ShouldReturnNoContentAndDeleteComment()
        {
            // Manager crée le projet.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CDM");
            var issue = await CreateIssueAsync(project.Id, "Manager Delete Issue");

            // Developer crée le commentaire.
            await AuthenticateAsync(
                "comment-manager-target@test.com",
                "comment-manager-target");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "comment-manager-target@test.com",
                ProjectRole.Developer);

            var comment = await CreateCommentAsync(
                issue.Id,
                "Developer comment");

            // Revenir au Manager initial.
            var managerAuth = await AuthenticateAsync(
                "comment-manager@test.com",
                "comment-manager");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "comment-manager@test.com",
                ProjectRole.Manager);

            var response = await Client.DeleteAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await CommentExistsAsync(comment.Id);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_WhenUserIsAdmin_ShouldReturnNoContentAndDeleteComment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CDAD");
            var issue = await CreateIssueAsync(project.Id, "Admin Delete Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Comment for admin");

            await AuthenticateAsync(
                "comment-admin@test.com",
                "comment-admin",
                SystemRole.Admin);

            var response = await Client.DeleteAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await CommentExistsAsync(comment.Id);

            exists.Should().BeFalse();
        }
    }
}