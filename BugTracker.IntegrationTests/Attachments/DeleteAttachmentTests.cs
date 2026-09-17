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
    public class DeleteAttachmentTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteAttachmentTests(CustomWebApplicationFactory factory) : base(factory)
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
            var bytes = Encoding.UTF8.GetBytes("Delete test file");

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

        private async Task<bool> AttachmentExistsAsync(Guid attachmentId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext
                    .Set<BugTracker.Domain.Entities.Attachment>()
                    .AnyAsync(a => a.Id == attachmentId);
            });

            return exists;
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.DeleteAsync(
                $"/api/attachments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenAttachmentDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.DeleteAsync(
                $"/api/attachments/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenUserIsNotUploaderOrManager_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AXF");
            var issue = await CreateIssueAsync(project.Id, "Delete Permission");

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "protected.txt");

            await AuthenticateAsync(
                "attachment-other@test.com",
                "attachment-other");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "attachment-other@test.com",
                ProjectRole.Developer);

            var response = await Client.DeleteAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var exists = await AttachmentExistsAsync(attachment.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenUserIsUploader_ShouldReturnNoContentAndDeleteAttachment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AXU");
            var issue = await CreateIssueAsync(project.Id, "Uploader Delete");

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "uploader.txt");

            var response = await Client.DeleteAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await AttachmentExistsAsync(attachment.Id);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_WhenUserIsProjectManager_ShouldReturnNoContentAndDeleteAttachment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AXM");
            var issue = await CreateIssueAsync(project.Id, "Manager Delete");

            // Developer devient l'uploader.
            await AuthenticateAsync(
                "attachment-dev-uploader@test.com",
                "attachment-dev-uploader");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "attachment-dev-uploader@test.com",
                ProjectRole.Developer);

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "manager-delete.txt");

            // Un autre utilisateur devient Manager.
            await AuthenticateAsync(
                "attachment-manager@test.com",
                "attachment-manager");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "attachment-manager@test.com",
                ProjectRole.Manager);

            var response = await Client.DeleteAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await AttachmentExistsAsync(attachment.Id);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_WhenUserIsAdmin_ShouldReturnNoContentAndDeleteAttachment()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AXA");
            var issue = await CreateIssueAsync(project.Id, "Admin Delete");

            var attachment = await UploadAttachmentAsync(
                issue.Id,
                "admin-delete.txt");

            await AuthenticateAsync(
                "attachment-admin@test.com",
                "attachment-admin",
                SystemRole.Admin);

            var response = await Client.DeleteAsync(
                $"/api/attachments/{attachment.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await AttachmentExistsAsync(attachment.Id);

            exists.Should().BeFalse();
        }
    }
}