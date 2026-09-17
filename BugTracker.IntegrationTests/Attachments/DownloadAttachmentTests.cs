using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Attachments
{
    public class DownloadAttachmentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DownloadAttachmentTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<AttachmentDto> UploadAttachmentAsync(Guid issueId)
        {
            var bytes = Encoding.UTF8.GetBytes("Download test");

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(bytes);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "download.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{issueId}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<AttachmentDto>())!;
        }

        [Fact]
        public async Task Download_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/attachments/{Guid.NewGuid()}/download");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Download_WhenAttachmentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                $"/api/attachments/{Guid.NewGuid()}/download");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Download_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ADF");
            var issue = await CreateIssueAsync(project.Id, "Private Download");

            var attachment = await UploadAttachmentAsync(issue.Id);

            await AuthenticateAsync(
                "download-outsider@test.com",
                "download-outsider");

            var response = await Client.GetAsync(
                $"/api/attachments/{attachment.Id}/download");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Download_WhenUserIsProjectMember_ShouldReturnOkAndDownloadUrl()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ADV");
            var issue = await CreateIssueAsync(project.Id, "Download Issue");

            var attachment = await UploadAttachmentAsync(issue.Id);

            var response = await Client.GetAsync(
                $"/api/attachments/{attachment.Id}/download");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            json.TryGetProperty("downloadUrl", out var downloadUrlProperty)
                .Should().BeTrue();

            var downloadUrl = downloadUrlProperty.GetString();

            downloadUrl.Should().NotBeNullOrWhiteSpace();

            Uri.TryCreate(
                downloadUrl,
                UriKind.Absolute,
                out _).Should().BeTrue();
        }
    }
}