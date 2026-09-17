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
    public class GetByIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIssueTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task GetByIssue_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/issues/{Guid.NewGuid()}/comments");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                $"/api/issues/{Guid.NewGuid()}/comments");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueHasNoComments_ShouldReturnOkAndEmptyList()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CGE");
            var issue = await CreateIssueAsync(project.Id, "Empty Comments Issue");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/comments");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var comments = await response.Content
                .ReadFromJsonAsync<IEnumerable<CommentDto>>();

            comments.Should().NotBeNull();
            comments.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIssue_WhenIssueHasComments_ShouldReturnAllComments()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CGL");
            var issue = await CreateIssueAsync(project.Id, "Comments Issue");

            var comment1 = await CreateCommentAsync(
                issue.Id,
                "First comment");

            var comment2 = await CreateCommentAsync(
                issue.Id,
                "Second comment");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/comments");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var comments = await response.Content
                .ReadFromJsonAsync<IEnumerable<CommentDto>>();

            comments.Should().NotBeNull();

            var list = comments!.ToList();

            list.Should().HaveCount(2);

            list.Should().Contain(c =>
                c.Id == comment1.Id &&
                c.Content == "First comment");

            list.Should().Contain(c =>
                c.Id == comment2.Id &&
                c.Content == "Second comment");

            list.Should().OnlyContain(c =>
                c.IssueId == issue.Id);
        }

        [Fact]
        public async Task GetByIssue_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CGO");
            var issue = await CreateIssueAsync(project.Id, "Private Issue");

            await CreateCommentAsync(
                issue.Id,
                "Private comment");

            await AuthenticateAsync(
                "comments-outsider@test.com",
                "comments-outsider");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/comments");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}