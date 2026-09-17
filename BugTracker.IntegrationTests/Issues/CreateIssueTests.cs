using BugTracker.Application.DTOs.Epics;
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
    public class CreateIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateIssueTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Guid> CreateUserAsync(string email, string username)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;
            });

            return userId;
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

        private async Task<Guid> CreateUserAndMemberAsync(Guid projectId, string email, string username, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

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

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Create_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new CreateIssueDto
            {
                Title = "Authentication bug",
                Type = IssueType.Bug
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICF");

            // User 2 est authentifié mais n'est pas membre.
            await AuthenticateAsync(
                "issue-outsider@test.com",
                "issue-outsider-user");

            var dto = new CreateIssueDto
            {
                Title = "Forbidden issue",
                Description = "Outsider must not create this issue",
                Type = IssueType.Task
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i =>
                        i.ProjectId == project.Id &&
                        i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "issue-admin@test.com",
                "issue-admin-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();

            var dto = new CreateIssueDto
            {
                Title = "Unknown project issue",
                Type = IssueType.Task
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{unknownProjectId}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICSN");

            var dto = new CreateIssueDto
            {
                Title = "Missing sprint issue",
                Type = IssueType.Task,
                SprintId = Guid.NewGuid()
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenSprintBelongsToAnotherProject_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("ICSP1");
            var project2 = await CreateProjectAsync("ICSP2");

            var sprintFromProject2 = await CreateSprintAsync(
                project2.Id,
                "Project 2 Sprint");

            var dto = new CreateIssueDto
            {
                Title = "Cross project sprint issue",
                Type = IssueType.Task,
                SprintId = sprintFromProject2.Id
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project1.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICEN");

            var dto = new CreateIssueDto
            {
                Title = "Missing epic issue",
                Type = IssueType.Feature,
                EpicId = Guid.NewGuid()
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenEpicBelongsToAnotherProject_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("ICEP1");
            var project2 = await CreateProjectAsync("ICEP2");

            var epicFromProject2 = await CreateEpicAsync(
                project2.Id,
                "Project 2 Epic");

            var dto = new CreateIssueDto
            {
                Title = "Cross project epic issue",
                Type = IssueType.Feature,
                EpicId = epicFromProject2.Id
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project1.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenEpicIsArchived_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICEA");

            var epic = await CreateEpicAsync(
                project.Id,
                "Archived Epic");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedEpic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epic.Id);

                savedEpic.Status = EpicStatus.Archived;

                await dbContext.SaveChangesAsync();
            });

            var dto = new CreateIssueDto
            {
                Title = "Archived epic issue",
                Type = IssueType.Task,
                EpicId = epic.Id
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenAssigneeIsNotProjectMember_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICAN");

            var outsiderId = await CreateUserAsync(
                "issue-assignee-outsider@test.com",
                "issue-assignee-outsider");

            var dto = new CreateIssueDto
            {
                Title = "Invalid assignee issue",
                Type = IssueType.Bug,
                AssigneeId = outsiderId
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var exists = await dbContext.Set<Issue>()
                    .AnyAsync(i => i.Title == dto.Title);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenDeveloperIsProjectMember_ShouldReturnCreatedAndSetDeveloperAsReporter()
        {
            // Arrange

            // User 1 crée le projet.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICD");

            // User 2 sera Developer du projet.
            await AuthenticateAsync(
                "issue-developer@test.com",
                "issue-developer-user");

            var developerId = await AddExistingUserAsMemberAsync(
                project.Id,
                "issue-developer@test.com",
                ProjectRole.Developer);

            var dto = new CreateIssueDto
            {
                Title = "Developer created issue",
                Description = "A project Developer can create an issue",
                Type = IssueType.Bug
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var issue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            issue!.ProjectId.Should().Be(project.Id);
            issue.ReporterId.Should().Be(developerId);
            issue.Title.Should().Be(dto.Title);
            issue.Description.Should().Be(dto.Description);
            issue.Type.Should().Be(IssueType.Bug);

            issue.Status.Should().Be(IssueStatus.Todo);
            issue.Priority.Should().Be(Priority.Medium);
            issue.DisplayOrder.Should().Be(0);

            issue.EpicId.Should().BeNull();
            issue.SprintId.Should().BeNull();
            issue.AssigneeId.Should().BeNull();

            var exists = await IssueExistsAsync(issue.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Create_WhenOptionalReferencesAreValid_ShouldReturnCreatedAndPersistAllValues()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ICV");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Issue Sprint");

            var epic = await CreateEpicAsync(
                project.Id,
                "Issue Epic");

            var assigneeId = await CreateUserAndMemberAsync(
                project.Id,
                "issue-assignee@test.com",
                "issue-assignee-user",
                ProjectRole.Developer);

            var dueDate = DateTime.UtcNow.Date.AddDays(10);

            var dto = new CreateIssueDto
            {
                Title = "Complete integration issue",
                Description = "Issue with all optional references",
                Type = IssueType.Feature,
                Priority = Priority.High,
                StoryPoints = 8,
                DueDate = dueDate,
                EpicId = epic.Id,
                SprintId = sprint.Id,
                AssigneeId = assigneeId
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/issues",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var issue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            issue!.Id.Should().NotBe(Guid.Empty);
            issue.ProjectId.Should().Be(project.Id);

            issue.Title.Should().Be(dto.Title);
            issue.Description.Should().Be(dto.Description);

            issue.Type.Should().Be(IssueType.Feature);
            issue.Status.Should().Be(IssueStatus.Todo);
            issue.Priority.Should().Be(Priority.High);

            issue.StoryPoints.Should().Be(8);
            issue.DueDate.Should().Be(dueDate);

            issue.EpicId.Should().Be(epic.Id);
            issue.SprintId.Should().Be(sprint.Id);
            issue.AssigneeId.Should().Be(assigneeId);

            issue.DisplayOrder.Should().Be(0);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.ProjectId.Should().Be(project.Id);
                savedIssue.Title.Should().Be(dto.Title);
                savedIssue.Description.Should().Be(dto.Description);

                savedIssue.Type.Should().Be(IssueType.Feature);
                savedIssue.Status.Should().Be(IssueStatus.Todo);
                savedIssue.Priority.Should().Be(Priority.High);

                savedIssue.StoryPoints.Should().Be(8);
                savedIssue.DueDate.Should().Be(dueDate);

                savedIssue.EpicId.Should().Be(epic.Id);
                savedIssue.SprintId.Should().Be(sprint.Id);
                savedIssue.AssigneeId.Should().Be(assigneeId);

                savedIssue.DisplayOrder.Should().Be(0);
            });
        }
    }
}