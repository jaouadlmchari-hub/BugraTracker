using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.ProjectMembers
{
    public class AddMemberTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public AddMemberTests(CustomWebApplicationFactory factory) : base(factory)
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


        private async Task<Guid> CreateUserAsync(string email, string username, bool isActive = true)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = "not-used-here",
                    IsActive = isActive
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


        private async Task<bool> MembershipExistsAsync(Guid projectId, Guid userId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext.Set<ProjectMember>()
                    .AnyAsync(pm =>
                        pm.ProjectId == projectId &&
                        pm.UserId == userId);
            });

            return exists;
        }


        private async Task<int> GetMembershipCountAsync(Guid projectId, Guid userId)
        {
            var count = 0;

            await ExecuteDbContextAsync(async dbContext =>
            {
                count = await dbContext.Set<ProjectMember>()
                    .CountAsync(pm =>
                        pm.ProjectId == projectId &&
                        pm.UserId == userId);
            });

            return count;
        }


        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task AddMember_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new AddProjectMemberDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/members",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task AddMember_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet → Manager
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMD");

            // User 2 devient l'utilisateur courant
            await AuthenticateAsync(
                "developer-add@test.com",
                "developer-add-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "developer-add@test.com",
                ProjectRole.Developer);

            var targetUserId = await CreateUserAsync(
                "target-add@test.com",
                "target-add-user");

            var dto = new AddProjectMemberDto
            {
                UserId = targetUserId,
                Role = ProjectRole.QA
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                targetUserId);

            membershipExists.Should().BeFalse();
        }


        [Fact]
        public async Task AddMember_WhenUserIsManagerAndDataIsValid_ShouldReturnCreatedAndAddMember()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMV");

            var newMemberId = await CreateUserAsync(
                "new-member@test.com",
                "new-member-user");

            var dto = new AddProjectMemberDto
            {
                UserId = newMemberId,
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var member = await response.Content
                .ReadFromJsonAsync<ProjectMemberDto>();

            member.Should().NotBeNull();
            member!.UserId.Should().Be(newMemberId);
            member.Username.Should().Be("new-member-user");
            member.Role.Should().Be(ProjectRole.Developer);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var membership = await dbContext.Set<ProjectMember>()
                    .SingleAsync(pm =>
                        pm.ProjectId == project.Id &&
                        pm.UserId == newMemberId);

                membership.Role.Should().Be(ProjectRole.Developer);
            });
        }


        [Fact]
        public async Task AddMember_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "admin-add@test.com",
                "admin-add-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();

            var dto = new AddProjectMemberDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{unknownProjectId}/members",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task AddMember_WhenProjectIsArchived_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMA");

            var targetUserId = await CreateUserAsync(
                "archived-target@test.com",
                "archived-target-user");

            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var dto = new AddProjectMemberDto
            {
                UserId = targetUserId,
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                targetUserId);

            membershipExists.Should().BeFalse();
        }


        [Fact]
        public async Task AddMember_WhenTargetUserDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMN");

            var unknownUserId = Guid.NewGuid();

            var dto = new AddProjectMemberDto
            {
                UserId = unknownUserId,
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                unknownUserId);

            membershipExists.Should().BeFalse();
        }


        [Fact]
        public async Task AddMember_WhenTargetUserIsInactive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMI");

            var inactiveUserId = await CreateUserAsync(
                "inactive-member@test.com",
                "inactive-member-user",
                false);

            var dto = new AddProjectMemberDto
            {
                UserId = inactiveUserId,
                Role = ProjectRole.Developer
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                inactiveUserId);

            membershipExists.Should().BeFalse();
        }


        [Fact]
        public async Task AddMember_WhenUserIsAlreadyMember_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("AMDUP");

            var targetUserId = await CreateUserAsync(
                "duplicate-member@test.com",
                "duplicate-member-user");

            var dto = new AddProjectMemberDto
            {
                UserId = targetUserId,
                Role = ProjectRole.Developer
            };

            var firstResponse = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var secondResponse = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/members",
                dto);

            // Assert
            secondResponse.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipCount = await GetMembershipCountAsync(
                project.Id,
                targetUserId);

            membershipCount.Should().Be(1);
        }
    }
}