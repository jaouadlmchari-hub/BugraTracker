using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Comments
{
    public class GetByIdTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIdTests(CustomWebApplicationFactory factory) : base(factory)
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

        [Fact]
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/comments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenCommentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                $"/api/comments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOkAndComment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CGI");
            var issue = await CreateIssueAsync(project.Id, "Comment Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Integration test comment");

            var response = await Client.GetAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<CommentDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(comment.Id);
            result.IssueId.Should().Be(issue.Id);
            result.AuthorId.Should().Be(comment.AuthorId);
            result.Content.Should().Be("Integration test comment");
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CGF");
            var issue = await CreateIssueAsync(project.Id, "Private Issue");

            var comment = await CreateCommentAsync(
                issue.Id,
                "Private comment");

            await AuthenticateAsync(
                "comment-outsider@test.com",
                "comment-outsider");

            var response = await Client.GetAsync(
                $"/api/comments/{comment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}