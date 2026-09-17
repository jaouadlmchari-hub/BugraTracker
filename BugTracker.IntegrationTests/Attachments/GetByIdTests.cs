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

        private async Task<AttachmentDto> UploadAttachmentAsync(Guid issueId, string filename)
        {
            var bytes = Encoding.UTF8.GetBytes("Integration test attachment");

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
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/attachments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenAttachmentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                $"/api/attachments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOkAndAttachment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AGI");
            var issue = await CreateIssueAsync(project.Id, "Attachment Issue");

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "notes.txt");

            var response = await Client.GetAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<AttachmentDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(attachment.Id);
            result.IssueId.Should().Be(issue.Id);
            result.UploaderId.Should().Be(attachment.UploaderId);
            result.Filename.Should().Be("notes.txt");
            result.MimeType.Should().Be("text/plain");
            result.SizeBytes.Should().BeGreaterThan(0);
            result.DownloadUrl.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AGF");
            var issue = await CreateIssueAsync(project.Id, "Private Attachment Issue");

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "private.txt");

            await AuthenticateAsync(
                "attachment-outsider@test.com",
                "attachment-outsider");

            var response = await Client.GetAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}