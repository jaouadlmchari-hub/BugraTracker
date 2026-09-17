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
    public class UpdateCommentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateCommentTests(CustomWebApplicationFactory factory) : base(factory)
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

        [Fact]
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new UpdateCommentDto
            {
                Content = "Updated"
            };

            var response = await Client.PutAsJsonAsync(
                $"/api/comments/{Guid.NewGuid()}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenCommentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new UpdateCommentDto
            {
                Content = "Updated"
            };

            var response = await Client.PutAsJsonAsync(
                $"/api/comments/{Guid.NewGuid()}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WhenUserIsNotAuthor_ShouldReturnForbiddenAndKeepCommentUnchanged()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CUF");
            var issue = await CreateIssueAsync(project.Id, "Update Comment Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Original comment");

            await AuthenticateAsync(
                "comment-non-author@test.com",
                "comment-non-author");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "comment-non-author@test.com",
                ProjectRole.Developer);

            var dto = new UpdateCommentDto
            {
                Content = "Illegal update"
            };

            var response = await Client.PutAsJsonAsync(
                $"/api/comments/{comment.Id}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedComment = await dbContext
                    .Set<BugTracker.Domain.Entities.Comment>()
                    .SingleAsync(c => c.Id == comment.Id);

                savedComment.Content.Should().Be("Original comment");
            });
        }

        [Fact]
        public async Task Update_WhenUserIsAuthor_ShouldReturnOkAndUpdateComment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CUV");
            var issue = await CreateIssueAsync(project.Id, "Author Update Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Original content");

            var dto = new UpdateCommentDto
            {
                Content = "Updated content"
            };

            var response = await Client.PutAsJsonAsync(
                $"/api/comments/{comment.Id}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedComment = await response.Content
                .ReadFromJsonAsync<CommentDto>();

            updatedComment.Should().NotBeNull();

            updatedComment!.Id.Should().Be(comment.Id);
            updatedComment.IssueId.Should().Be(issue.Id);
            updatedComment.AuthorId.Should().Be(comment.AuthorId);
            updatedComment.Content.Should().Be("Updated content");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedComment = await dbContext
                    .Set<BugTracker.Domain.Entities.Comment>()
                    .SingleAsync(c => c.Id == comment.Id);

                savedComment.Content.Should().Be("Updated content");
            });
        }
    }
}