using BugTracker.Application.DTOs.Epics;
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
    public class UpdateIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateIssueTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<EpicDto> CreateEpicAsync(Guid projectId, string title)
        {
            var dto = new CreateEpicDto
            {
                Title = title,
                Description = $"Description for {title}",
                ColorCode = "#3B82F6"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/epics",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var epic = await response.Content.ReadFromJsonAsync<EpicDto>();

            epic.Should().NotBeNull();

            return epic!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, Guid? epicId = null)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Description = $"Description for {title}",
                Type = IssueType.Task,
                Priority = Priority.Medium,
                StoryPoints = 3,
                DueDate = DateTime.UtcNow.Date.AddDays(7),
                EpicId = epicId
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

        private async Task SetEpicStatusAsync(Guid epicId, EpicStatus status)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var epic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epicId);

                epic.Status = status;

                await dbContext.SaveChangesAsync();
            });
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var dto = new UpdateIssueDto
            {
                Title = "Updated Issue",
                Description = "Updated description",
                Type = IssueType.Bug,
                Priority = Priority.High,
                StoryPoints = 5
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issueId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownIssueId = Guid.NewGuid();

            var dto = new UpdateIssueDto
            {
                Title = "Unknown Issue",
                Description = "Should not be updated",
                Type = IssueType.Task,
                Priority = Priority.Medium
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{unknownIssueId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WhenUserIsProjectMemberWithoutEditRights_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Issue.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IUF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Original Issue");

            // User 2 est membre Developer,
            // mais n'est ni Reporter, ni Assignee, ni Manager.
            await AuthenticateAsync(
                "issue-update-developer@test.com",
                "issue-update-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "issue-update-developer@test.com",
                ProjectRole.Developer);

            var dto = new UpdateIssueDto
            {
                Title = "Forbidden Update",
                Description = "Must not be applied",
                Type = IssueType.Bug,
                Priority = Priority.Critical,
                StoryPoints = 13
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be("Original Issue");
                savedIssue.Type.Should().Be(IssueType.Task);
                savedIssue.Priority.Should().Be(Priority.Medium);
                savedIssue.StoryPoints.Should().Be(3);
            });
        }

        [Fact]
        public async Task Update_WhenNewEpicDoesNotExist_ShouldReturnNotFoundAndKeepIssueUnchanged()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IUEN");

            var issue = await CreateIssueAsync(
                project.Id,
                "Issue With Missing Epic");

            var dto = new UpdateIssueDto
            {
                Title = "Changed Title",
                Description = "Changed description",
                Type = IssueType.Feature,
                Priority = Priority.High,
                StoryPoints = 8,
                EpicId = Guid.NewGuid()
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be("Issue With Missing Epic");
                savedIssue.EpicId.Should().BeNull();
            });
        }

        [Fact]
        public async Task Update_WhenNewEpicBelongsToAnotherProject_ShouldReturnUnprocessableEntityAndKeepIssueUnchanged()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("IUEP1");
            var project2 = await CreateProjectAsync("IUEP2");

            var issue = await CreateIssueAsync(
                project1.Id,
                "Project 1 Issue");

            var project2Epic = await CreateEpicAsync(
                project2.Id,
                "Project 2 Epic");

            var dto = new UpdateIssueDto
            {
                Title = "Cross Project Update",
                Description = "Should fail",
                Type = IssueType.Feature,
                Priority = Priority.High,
                StoryPoints = 8,
                EpicId = project2Epic.Id
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be("Project 1 Issue");
                savedIssue.EpicId.Should().BeNull();
            });
        }

        [Fact]
        public async Task Update_WhenNewEpicIsArchived_ShouldReturnUnprocessableEntityAndKeepIssueUnchanged()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IUEA");

            var issue = await CreateIssueAsync(
                project.Id,
                "Issue Before Archived Epic");

            var epic = await CreateEpicAsync(
                project.Id,
                "Archived Epic");

            await SetEpicStatusAsync(
                epic.Id,
                EpicStatus.Archived);

            var dto = new UpdateIssueDto
            {
                Title = "Changed Issue",
                Description = "Should not be saved",
                Type = IssueType.Bug,
                Priority = Priority.Critical,
                StoryPoints = 13,
                EpicId = epic.Id
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be("Issue Before Archived Epic");
                savedIssue.EpicId.Should().BeNull();
            });
        }

        [Fact]
        public async Task Update_WhenReporterIsDeveloper_ShouldReturnOkAndUpdateIssue()
        {
            // Arrange

            // Manager crée le projet et les Epics.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IUR");

            var originalEpic = await CreateEpicAsync(
                project.Id,
                "Original Epic");

            var newEpic = await CreateEpicAsync(
                project.Id,
                "New Epic");

            // User 2 devient Developer.
            await AuthenticateAsync(
                "issue-reporter@test.com",
                "issue-reporter-user");

            var reporterId = await AddExistingUserAsMemberAsync(
                project.Id,
                "issue-reporter@test.com",
                ProjectRole.Developer);

            // Le Developer crée lui-même l'Issue :
            // il devient donc Reporter.
            var issue = await CreateIssueAsync(
                project.Id,
                "Reporter Original Issue",
                originalEpic.Id);

            issue.ReporterId.Should().Be(reporterId);

            var newDueDate = DateTime.UtcNow.Date.AddDays(20);

            var dto = new UpdateIssueDto
            {
                Title = "Reporter Updated Issue",
                Description = "Updated by its Reporter",
                Type = IssueType.Bug,
                Priority = Priority.Critical,
                StoryPoints = 13,
                DueDate = newDueDate,
                EpicId = newEpic.Id
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedIssue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            updatedIssue.Should().NotBeNull();

            updatedIssue!.Id.Should().Be(issue.Id);
            updatedIssue.ProjectId.Should().Be(project.Id);
            updatedIssue.ReporterId.Should().Be(reporterId);

            updatedIssue.Title.Should().Be(dto.Title);
            updatedIssue.Description.Should().Be(dto.Description);

            updatedIssue.Type.Should().Be(IssueType.Bug);
            updatedIssue.Priority.Should().Be(Priority.Critical);
            updatedIssue.StoryPoints.Should().Be(13);
            updatedIssue.DueDate.Should().Be(newDueDate);
            updatedIssue.EpicId.Should().Be(newEpic.Id);

            // UpdateAsync ne change pas le Status.
            updatedIssue.Status.Should().Be(IssueStatus.Todo);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be(dto.Title);
                savedIssue.Description.Should().Be(dto.Description);
                savedIssue.Type.Should().Be(IssueType.Bug);
                savedIssue.Priority.Should().Be(Priority.Critical);
                savedIssue.StoryPoints.Should().Be(13);
                savedIssue.DueDate.Should().Be(newDueDate);
                savedIssue.EpicId.Should().Be(newEpic.Id);
                savedIssue.Status.Should().Be(IssueStatus.Todo);
            });
        }

        [Fact]
        public async Task Update_WhenEpicIdIsSetToNull_ShouldReturnOkAndRemoveIssueFromEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IURN");

            var epic = await CreateEpicAsync(
                project.Id,
                "Epic To Remove");

            var issue = await CreateIssueAsync(
                project.Id,
                "Issue With Epic",
                epic.Id);

            issue.EpicId.Should().Be(epic.Id);

            var dto = new UpdateIssueDto
            {
                Title = "Issue Without Epic",
                Description = "Epic has been removed",
                Type = IssueType.Task,
                Priority = Priority.Medium,
                StoryPoints = 5,
                DueDate = issue.DueDate,
                EpicId = null
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedIssue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            updatedIssue.Should().NotBeNull();
            updatedIssue!.EpicId.Should().BeNull();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.EpicId.Should().BeNull();
            });
        }

        [Fact]
        public async Task Update_WhenUserIsManagerAndDataIsValid_ShouldReturnOkAndPersistChanges()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IUV");

            var issue = await CreateIssueAsync(
                project.Id,
                "Manager Original Issue");

            var newDueDate = DateTime.UtcNow.Date.AddDays(30);

            var dto = new UpdateIssueDto
            {
                Title = "Manager Updated Issue",
                Description = "Updated by project Manager",
                Type = IssueType.Feature,
                Priority = Priority.High,
                StoryPoints = 8,
                DueDate = newDueDate
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/issues/{issue.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedIssue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            updatedIssue.Should().NotBeNull();

            updatedIssue!.Id.Should().Be(issue.Id);
            updatedIssue.ProjectId.Should().Be(project.Id);

            updatedIssue.Title.Should().Be(dto.Title);
            updatedIssue.Description.Should().Be(dto.Description);
            updatedIssue.Type.Should().Be(IssueType.Feature);
            updatedIssue.Priority.Should().Be(Priority.High);
            updatedIssue.StoryPoints.Should().Be(8);
            updatedIssue.DueDate.Should().Be(newDueDate);

            updatedIssue.Status.Should().Be(IssueStatus.Todo);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.Title.Should().Be(dto.Title);
                savedIssue.Description.Should().Be(dto.Description);
                savedIssue.Type.Should().Be(IssueType.Feature);
                savedIssue.Priority.Should().Be(Priority.High);
                savedIssue.StoryPoints.Should().Be(8);
                savedIssue.DueDate.Should().Be(newDueDate);

                savedIssue.Status.Should().Be(IssueStatus.Todo);
            });
        }
    }
}