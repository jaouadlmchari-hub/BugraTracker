using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class DeleteIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteIssueTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<bool> IssueExistsAsync(Guid issueId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Id == issueId);
            });

            return exists;
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.DeleteAsync(
                $"/api/issues/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.DeleteAsync(
                $"/api/issues/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenUserIsDeveloper_ShouldReturnForbiddenAndKeepIssue()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IDF");
            var issue = await CreateIssueAsync(project.Id, "Protected Issue");

            await AuthenticateAsync(
                "delete-issue-developer@test.com",
                "delete-issue-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "delete-issue-developer@test.com",
                ProjectRole.Developer);

            var response = await Client.DeleteAsync(
                $"/api/issues/{issue.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var exists = await IssueExistsAsync(issue.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenUserIsManager_ShouldReturnNoContentAndDeleteIssue()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IDV");
            var issue = await CreateIssueAsync(project.Id, "Issue To Delete");

            var existsBeforeDelete = await IssueExistsAsync(issue.Id);

            existsBeforeDelete.Should().BeTrue();

            var response = await Client.DeleteAsync(
                $"/api/issues/{issue.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var existsAfterDelete = await IssueExistsAsync(issue.Id);

            existsAfterDelete.Should().BeFalse();
        }
    }
}