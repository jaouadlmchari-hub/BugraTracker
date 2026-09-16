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
    public class ChangeRoleTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeRoleTests(CustomWebApplicationFactory factory) : base(factory)
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

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            return project!;
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


        private async Task<ProjectRole> GetMemberRoleAsync(Guid projectId, Guid userId)
        {
            ProjectRole role = default;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var membership = await dbContext.Set<ProjectMember>()
                    .SingleAsync(pm =>
                        pm.ProjectId == projectId &&
                        pm.UserId == userId);

                role = membership.Role;
            });

            return role;
        }


        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task ChangeRole_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{projectId}/members/{userId}/role",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task ChangeRole_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRD");

            await AuthenticateAsync(
                "developer-role@test.com",
                "developer-role-user");

            var developerId = await AddExistingUserAsMemberAsync(
                project.Id,
                "developer-role@test.com",
                ProjectRole.Developer);

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{developerId}/role",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var role = await GetMemberRoleAsync(
                project.Id,
                developerId);

            role.Should().Be(ProjectRole.Developer);
        }


        [Fact]
        public async Task ChangeRole_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "admin-role@test.com",
                "admin-role-user",
                SystemRole.Admin);

            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{projectId}/members/{userId}/role",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task ChangeRole_WhenProjectIsArchived_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRA");

            var memberId = await CreateUserAndMemberAsync(
                project.Id,
                "archived-role@test.com",
                "archived-role-user",
                ProjectRole.Developer);

            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            archiveResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{memberId}/role",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var role = await GetMemberRoleAsync(
                project.Id,
                memberId);

            role.Should().Be(ProjectRole.Developer);
        }


        [Fact]
        public async Task ChangeRole_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRM");

            var unknownUserId = Guid.NewGuid();

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{unknownUserId}/role",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task ChangeRole_WhenMemberAlreadyHasRequestedRole_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRS");

            var memberId = await CreateUserAndMemberAsync(
                project.Id,
                "same-role@test.com",
                "same-role-user",
                ProjectRole.Developer);

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.Developer
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{memberId}/role",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var role = await GetMemberRoleAsync(
                project.Id,
                memberId);

            role.Should().Be(ProjectRole.Developer);
        }


        [Fact]
        public async Task ChangeRole_WhenRemovingLastManager_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRL");

            Guid managerId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var manager = await dbContext.Set<User>()
                    .SingleAsync(
                        u => u.Email == "authenticated@test.com");

                managerId = manager.Id;
            });

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.Developer
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{managerId}/role",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var role = await GetMemberRoleAsync(
                project.Id,
                managerId);

            role.Should().Be(ProjectRole.Manager);
        }


        [Fact]
        public async Task ChangeRole_WhenUserIsManagerAndDataIsValid_ShouldReturnNoContentAndChangeRole()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CRV");

            var memberId = await CreateUserAndMemberAsync(
                project.Id,
                "role-target@test.com",
                "role-target-user",
                ProjectRole.Developer);

            var dto = new ChangeProjectMemberRoleDto
            {
                NewRole = ProjectRole.QA
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/members/{memberId}/role",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var role = await GetMemberRoleAsync(
                project.Id,
                memberId);

            role.Should().Be(ProjectRole.QA);
        }
    }
}