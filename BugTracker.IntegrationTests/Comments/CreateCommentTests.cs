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
    public class CreateCommentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateCommentTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Guid> AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                userId = user.Id;

                dbContext.Set<ProjectMember>().Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                });

                await dbContext.SaveChangesAsync();
            });

            return userId;
        }

        [Fact]
        public async Task Create_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new CreateCommentDto
            {
                Content = "Comment"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/comments",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new CreateCommentDto
            {
                Content = "Comment"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/comments",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CCF");
            var issue = await CreateIssueAsync(project.Id, "Private Issue");

            await AuthenticateAsync(
                "comment-create-outsider@test.com",
                "comment-create-outsider");

            var dto = new CreateCommentDto
            {
                Content = "Forbidden comment"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/issues/{issue.Id}/comments",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WhenDeveloperIsProjectMember_ShouldReturnCreatedAndSetAuthor()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CCV");
            var issue = await CreateIssueAsync(project.Id, "Commentable Issue");

            await AuthenticateAsync(
                "comment-author@test.com",
                "comment-author");

            var authorId = await AddExistingUserAsMemberAsync(
                project.Id,
                "comment-author@test.com",
                ProjectRole.Developer);

            var dto = new CreateCommentDto
            {
                Content = "Developer comment"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/issues/{issue.Id}/comments",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var comment = await response.Content
                .ReadFromJsonAsync<CommentDto>();

            comment.Should().NotBeNull();

            comment!.Id.Should().NotBe(Guid.Empty);
            comment.IssueId.Should().Be(issue.Id);
            comment.AuthorId.Should().Be(authorId);
            comment.Content.Should().Be(dto.Content);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedComment = await dbContext
                    .Set<BugTracker.Domain.Entities.Comment>()
                    .SingleAsync(c => c.Id == comment.Id);

                savedComment.IssueId.Should().Be(issue.Id);
                savedComment.AuthorId.Should().Be(authorId);
                savedComment.Content.Should().Be(dto.Content);
            });
        }
    }
}