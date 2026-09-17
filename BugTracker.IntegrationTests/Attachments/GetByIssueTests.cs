using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace BugTracker.IntegrationTests.Attachments
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

        private async Task<AttachmentDto> UploadAttachmentAsync(Guid issueId, string filename)
        {
            var bytes = Encoding.UTF8.GetBytes($"Content of {filename}");

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(bytes);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", filename);

            var response = await Client.PostAsync(
                $"/api/issues/{issueId}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<AttachmentDto>())!;
        }

        [Fact]
        public async Task GetByIssue_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/issues/{Guid.NewGuid()}/attachments");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                $"/api/issues/{Guid.NewGuid()}/attachments");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueHasNoAttachments_ShouldReturnOkAndEmptyList()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AGE");
            var issue = await CreateIssueAsync(project.Id, "Empty Attachments");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/attachments");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var attachments = await response.Content
                .ReadFromJsonAsync<IEnumerable<AttachmentDto>>();

            attachments.Should().NotBeNull();
            attachments.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIssue_WhenIssueHasAttachments_ShouldReturnAllAttachments()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AGL");
            var issue = await CreateIssueAsync(project.Id, "Attachments Issue");

            var attachment1 = await UploadAttachmentAsync(
                issue.Id,
                "one.txt");

            var attachment2 = await UploadAttachmentAsync(
                issue.Id,
                "two.txt");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/attachments");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var attachments = await response.Content
                .ReadFromJsonAsync<IEnumerable<AttachmentDto>>();

            attachments.Should().NotBeNull();

            var list = attachments!.ToList();

            list.Should().HaveCount(2);

            list.Should().Contain(a =>
                a.Id == attachment1.Id &&
                a.Filename == "one.txt");

            list.Should().Contain(a =>
                a.Id == attachment2.Id &&
                a.Filename == "two.txt");

            list.Should().OnlyContain(a =>
                a.IssueId == issue.Id);
        }

        [Fact]
        public async Task GetByIssue_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AGO");
            var issue = await CreateIssueAsync(project.Id, "Private Issue");

            await UploadAttachmentAsync(
                issue.Id,
                "private.txt");

            await AuthenticateAsync(
                "attachment-list-outsider@test.com",
                "attachment-list-outsider");

            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/attachments");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}