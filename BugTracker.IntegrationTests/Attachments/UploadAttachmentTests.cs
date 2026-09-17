using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace BugTracker.IntegrationTests.Attachments
{
    public class UploadAttachmentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UploadAttachmentTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task Upload_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            using var content = new MultipartFormDataContent();

            using var fileContent = new ByteArrayContent(
                Encoding.UTF8.GetBytes("file"));

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "test.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{Guid.NewGuid()}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Upload_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            using var content = new MultipartFormDataContent();

            using var fileContent = new ByteArrayContent(
                Encoding.UTF8.GetBytes("file"));

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "test.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{Guid.NewGuid()}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Upload_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AUF");
            var issue = await CreateIssueAsync(project.Id, "Private Upload");

            await AuthenticateAsync(
                "upload-outsider@test.com",
                "upload-outsider");

            using var content = new MultipartFormDataContent();

            using var fileContent = new ByteArrayContent(
                Encoding.UTF8.GetBytes("file"));

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "test.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{issue.Id}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Upload_WhenFileIsEmpty_ShouldReturnBadRequest()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AUE");
            var issue = await CreateIssueAsync(project.Id, "Empty File");

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Array.Empty<byte>());

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "empty.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{issue.Id}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Upload_WhenProjectMemberUploadsValidFile_ShouldReturnCreatedAndPersistMetadata()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AUV");
            var issue = await CreateIssueAsync(project.Id, "Valid Upload");

            await AuthenticateAsync(
                "attachment-uploader@test.com",
                "attachment-uploader");

            var uploaderId = await AddExistingUserAsMemberAsync(
                project.Id,
                "attachment-uploader@test.com",
                ProjectRole.Developer);

            var bytes = Encoding.UTF8.GetBytes(
                "BugTracker integration test attachment");

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(bytes);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("text/plain");

            content.Add(fileContent, "file", "bug.txt");

            var response = await Client.PostAsync(
                $"/api/issues/{issue.Id}/attachments",
                content);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var attachment = await response.Content
                .ReadFromJsonAsync<AttachmentDto>();

            attachment.Should().NotBeNull();

            attachment!.Id.Should().NotBe(Guid.Empty);
            attachment.IssueId.Should().Be(issue.Id);
            attachment.UploaderId.Should().Be(uploaderId);
            attachment.Filename.Should().Be("bug.txt");
            attachment.MimeType.Should().Be("text/plain");
            attachment.SizeBytes.Should().Be(bytes.Length);
            attachment.DownloadUrl.Should().NotBeNullOrWhiteSpace();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext
                    .Set<BugTracker.Domain.Entities.Attachment>()
                    .AnyAsync(a => a.Id == attachment.Id);

                exists.Should().BeTrue();
            });
        }
    }
}