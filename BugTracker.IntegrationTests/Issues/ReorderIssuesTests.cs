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
    public class ReorderIssuesTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ReorderIssuesTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<int> GetDisplayOrderAsync(Guid issueId)
        {
            var displayOrder = 0;

            await ExecuteDbContextAsync(async dbContext =>
            {
                displayOrder = await dbContext.Set<Issue>()
                    .Where(i => i.Id == issueId)
                    .Select(i => i.DisplayOrder)
                    .SingleAsync();
            });

            return displayOrder;
        }

        [Fact]
        public async Task Reorder_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>
                {
                    new()
                    {
                        IssueId = Guid.NewGuid(),
                        DisplayOrder = 1
                    }
                }
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Reorder_WhenItemsIsEmpty_ShouldReturnBadRequest()
        {
            await AuthenticateAsync();

            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>()
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Reorder_WhenDisplayOrderIsNegative_ShouldReturnBadRequest()
        {
            await AuthenticateAsync();

            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>
                {
                    new()
                    {
                        IssueId = Guid.NewGuid(),
                        DisplayOrder = -1
                    }
                }
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Reorder_WhenFirstIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>
                {
                    new()
                    {
                        IssueId = Guid.NewGuid(),
                        DisplayOrder = 1
                    }
                }
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Reorder_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IRF");
            var issue = await CreateIssueAsync(project.Id, "Issue");

            await AuthenticateAsync(
                "reorder-developer@test.com",
                "reorder-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "reorder-developer@test.com",
                ProjectRole.Developer);

            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>
                {
                    new()
                    {
                        IssueId = issue.Id,
                        DisplayOrder = 5
                    }
                }
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var order = await GetDisplayOrderAsync(issue.Id);

            order.Should().Be(issue.DisplayOrder);
        }

        [Fact]
        public async Task Reorder_WhenManagerProvidesValidItems_ShouldReturnNoContentAndUpdateDisplayOrders()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IRV");

            var issue1 = await CreateIssueAsync(project.Id, "Issue 1");
            var issue2 = await CreateIssueAsync(project.Id, "Issue 2");

            var dto = new ReorderIssuesDto
            {
                Items = new List<ReorderIssueItemDto>
                {
                    new()
                    {
                        IssueId = issue1.Id,
                        DisplayOrder = 10
                    },
                    new()
                    {
                        IssueId = issue2.Id,
                        DisplayOrder = 20
                    }
                }
            };

            var response = await Client.PatchAsJsonAsync(
                "/api/issues/reorder",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var issue1Order = await GetDisplayOrderAsync(issue1.Id);
            var issue2Order = await GetDisplayOrderAsync(issue2.Id);

            issue1Order.Should().Be(10);
            issue2Order.Should().Be(20);
        }
    }
}