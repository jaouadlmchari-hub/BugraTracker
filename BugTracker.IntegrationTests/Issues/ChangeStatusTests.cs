using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class ChangeStatusTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeStatusTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        // =========================================================
        // Helpers
        // =========================================================

        private async Task<ProjectDto> CreateProjectAsync(string key)
        {
            var dto = new CreateProjectDto
            {
                Name = $"Project {key}",
                Key = key,
                Description = $"Integration test project {key}"
            };

            var response = await Client.PostAsJsonAsync("/api/projects", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content.ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            return project!;
        }

        private async Task<SprintDto> CreateSprintAsync(Guid projectId, string name)
        {
            var dto = new CreateSprintDto
            {
                Name = name,
                Goal = $"Goal for {name}",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/sprints",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var sprint = await response.Content.ReadFromJsonAsync<SprintDto>();

            sprint.Should().NotBeNull();

            return sprint!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, IssueType type = IssueType.Task, Guid? sprintId = null)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Description = $"Description for {title}",
                Type = type,
                Priority = Priority.Medium,
                SprintId = sprintId
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var issue = await response.Content.ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            return issue!;
        }

        private async Task<Guid> AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                userId = user.Id;

                var membership = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                };

                dbContext.Set<ProjectMember>().Add(membership);

                await dbContext.SaveChangesAsync();
            });

            return userId;
        }

        private async Task SetIssueStatusAsync(Guid issueId, IssueStatus status)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var issue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issueId);

                issue.Status = status;

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task SetIssueAssigneeAsync(Guid issueId, Guid assigneeId)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var issue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issueId);

                issue.AssigneeId = assigneeId;

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task SetSprintCompletedAsync(Guid sprintId)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var sprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprintId);

                sprint.Status = SprintStatus.Completed;
                sprint.CompletedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task<IssueStatus> GetIssueStatusAsync(Guid issueId)
        {
            IssueStatus status = default;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var issue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issueId);

                status = issue.Status;
            });

            return status;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task ChangeStatus_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issueId}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeStatus_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownIssueId = Guid.NewGuid();

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{unknownIssueId}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangeStatus_WhenDeveloperIsNeitherAssigneeNorManager_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISDF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Protected Issue");

            await AuthenticateAsync(
                "status-developer@test.com",
                "status-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "status-developer@test.com",
                ProjectRole.Developer);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Fact]
        public async Task ChangeStatus_WhenDeveloperIsAssignee_ShouldAllowNormalTransition()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISA");

            var issue = await CreateIssueAsync(
                project.Id,
                "Assigned Issue");

            await AuthenticateAsync(
                "status-assignee@test.com",
                "status-assignee-user");

            var assigneeId = await AddExistingUserAsMemberAsync(
                project.Id,
                "status-assignee@test.com",
                ProjectRole.Developer);

            await SetIssueAssigneeAsync(
                issue.Id,
                assigneeId);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.InProgress);
        }

        [Fact]
        public async Task ChangeStatus_WhenQAChangesBugStatus_ShouldReturnNoContent()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISQB");

            var issue = await CreateIssueAsync(
                project.Id,
                "QA Bug",
                IssueType.Bug);

            await AuthenticateAsync(
                "status-qa@test.com",
                "status-qa-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "status-qa@test.com",
                ProjectRole.QA);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.InProgress);
        }

        [Fact]
        public async Task ChangeStatus_WhenQAChangesNonBugIssue_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISQF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Feature For QA",
                IssueType.Feature);

            await AuthenticateAsync(
                "status-qa-feature@test.com",
                "status-qa-feature-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "status-qa-feature@test.com",
                ProjectRole.QA);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Theory]
        [InlineData(IssueStatus.Todo, IssueStatus.InProgress)]
        [InlineData(IssueStatus.InProgress, IssueStatus.InReview)]
        [InlineData(IssueStatus.InReview, IssueStatus.Done)]
        [InlineData(IssueStatus.Done, IssueStatus.Todo)]
        public async Task ChangeStatus_WhenManagerUsesValidTransition_ShouldReturnNoContent(IssueStatus currentStatus, IssueStatus newStatus)
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync(
                $"ISV{(int)currentStatus}{(int)newStatus}");

            var issue = await CreateIssueAsync(
                project.Id,
                "Workflow Issue");

            await SetIssueStatusAsync(
                issue.Id,
                currentStatus);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = newStatus
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(newStatus);
        }

        [Fact]
        public async Task ChangeStatus_WhenTransitionIsInvalid_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISIT");

            var issue = await CreateIssueAsync(
                project.Id,
                "Invalid Transition Issue");

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.Done
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Fact]
        public async Task ChangeStatus_WhenNewStatusIsSameAsCurrentStatus_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISSS");

            var issue = await CreateIssueAsync(
                project.Id,
                "Same Status Issue");

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.Todo
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Fact]
        public async Task ChangeStatus_WhenStatusValueIsInvalid_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISIV");

            var issue = await CreateIssueAsync(
                project.Id,
                "Invalid Enum Issue");

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = (IssueStatus)999
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Fact]
        public async Task ChangeStatus_WhenIssueBelongsToCompletedSprint_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISCS");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Completed Sprint");

            var issue = await CreateIssueAsync(
                project.Id,
                "Completed Sprint Issue",
                IssueType.Task,
                sprint.Id);

            await SetSprintCompletedAsync(sprint.Id);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }

        [Fact]
        public async Task ChangeStatus_WhenAssigneeTriesToReopenDoneIssue_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISRFA");

            var issue = await CreateIssueAsync(
                project.Id,
                "Done Assigned Issue");

            await SetIssueStatusAsync(
                issue.Id,
                IssueStatus.Done);

            await AuthenticateAsync(
                "reopen-assignee@test.com",
                "reopen-assignee-user");

            var assigneeId = await AddExistingUserAsMemberAsync(
                project.Id,
                "reopen-assignee@test.com",
                ProjectRole.Developer);

            await SetIssueAssigneeAsync(
                issue.Id,
                assigneeId);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.Todo
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Done);
        }

        [Fact]
        public async Task ChangeStatus_WhenQAReopensBug_ShouldReturnNoContentAndSetTodo()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ISRQ");

            var issue = await CreateIssueAsync(
                project.Id,
                "Done Bug",
                IssueType.Bug);

            await SetIssueStatusAsync(
                issue.Id,
                IssueStatus.Done);

            await AuthenticateAsync(
                "reopen-qa@test.com",
                "reopen-qa-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "reopen-qa@test.com",
                ProjectRole.QA);

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.Todo
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await GetIssueStatusAsync(issue.Id);

            status.Should().Be(IssueStatus.Todo);
        }
    }
}