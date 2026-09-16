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
    public class RemoveMemberTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public RemoveMemberTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Guid> GetAuthenticatedUserIdAsync()
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u =>
                        u.Email == "authenticated@test.com");

                userId = user.Id;
            });

            return userId;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task RemoveMember_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{projectId}/members/{userId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RemoveMember_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RMD");

            var targetUserId = await CreateUserAndMemberAsync(
                project.Id,
                "remove-target@test.com",
                "remove-target-user",
                ProjectRole.QA);

            await AuthenticateAsync(
                "remove-developer@test.com",
                "remove-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "remove-developer@test.com",
                ProjectRole.Developer);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{targetUserId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Forbidden);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                targetUserId);

            membershipExists.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMember_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "admin-remove@test.com",
                "admin-remove-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{unknownProjectId}/members/{userId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RemoveMember_WhenProjectIsArchived_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RMA");

            var targetUserId = await CreateUserAndMemberAsync(
                project.Id,
                "archived-remove@test.com",
                "archived-remove-user",
                ProjectRole.Developer);

            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            archiveResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{targetUserId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                targetUserId);

            membershipExists.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMember_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RMN");

            var unknownUserId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{unknownUserId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RemoveMember_WhenTargetMemberIsProjectOwner_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RMO");

            var ownerId = await GetAuthenticatedUserIdAsync();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{ownerId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                ownerId);

            membershipExists.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMember_WhenTargetIsLastManager_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RML");

            var ownerId = await GetAuthenticatedUserIdAsync();

            var managerId = await CreateUserAndMemberAsync(
                project.Id,
                "last-manager@test.com",
                "last-manager-user",
                ProjectRole.Manager);

            // Préparer l'état métier :
            // le second membre devient le seul Manager.
            await ExecuteDbContextAsync(async dbContext =>
            {
                var ownerMembership = await dbContext.Set<ProjectMember>()
                    .SingleAsync(pm =>
                        pm.ProjectId == project.Id &&
                        pm.UserId == ownerId);

                ownerMembership.Role = ProjectRole.Developer;

                await dbContext.SaveChangesAsync();
            });

            // On utilise un Admin pour atteindre le service,
            // puisque l'Owner n'est maintenant plus Manager.
            await AuthenticateAsync(
                "admin-last-manager@test.com",
                "admin-last-manager-user",
                SystemRole.Admin);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{managerId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                managerId);

            membershipExists.Should().BeTrue();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var managerMembership = await dbContext.Set<ProjectMember>()
                    .SingleAsync(pm =>
                        pm.ProjectId == project.Id &&
                        pm.UserId == managerId);

                managerMembership.Role.Should()
                    .Be(ProjectRole.Manager);
            });
        }

        [Fact]
        public async Task RemoveMember_WhenUserIsManagerAndDataIsValid_ShouldReturnNoContentAndRemoveMember()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("RMV");

            var targetUserId = await CreateUserAndMemberAsync(
                project.Id,
                "valid-remove@test.com",
                "valid-remove-user",
                ProjectRole.Developer);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}/members/{targetUserId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var membershipExists = await MembershipExistsAsync(
                project.Id,
                targetUserId);

            membershipExists.Should().BeFalse();
        }
    }
}