using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BugTracker.IntegrationTests
{
    public class ProjectControllerTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ProjectControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Act
            var response = await Client.GetAsync("/api/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WhenUserIsAuthenticated_ShouldReturnOk()
        {
            // Arrange
            await AuthenticateAsync();

            // Act
            var response = await Client.GetAsync("/api/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_WhenDataIsValid_ShouldReturnCreatedAndCreateOwnerAsManager()
        {
            // Arrange
            await AuthenticateAsync();

            var dto = new CreateProjectDto
            {
                Name = "BugTracker Project",
                Key = "BT",
                Description = "Integration test project"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/projects", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == "authenticated@test.com");

                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Key == dto.Key);

                project.Name.Should().Be(dto.Name);
                project.Description.Should().Be(dto.Description);
                project.OwnerId.Should().Be(user.Id);

                var projectMember = await dbContext.Set<ProjectMember>()
                    .SingleAsync(pm =>
                        pm.ProjectId == project.Id &&
                        pm.UserId == user.Id);

                projectMember.Role.Should().Be(ProjectRole.Manager);
            });
        }

        [Fact]
        public async Task Create_WhenProjectKeyAlreadyExists_ShouldReturnConflict()
        {
            // Arrange
            await AuthenticateAsync();

            var firstProjectDto = new CreateProjectDto
            {
                Name = "First Project",
                Key = "DUP",
                Description = "First project"
            };

            var firstResponse = await Client.PostAsJsonAsync(
                "/api/projects",
                firstProjectDto);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var duplicateProjectDto = new CreateProjectDto
            {
                Name = "Second Project",
                Key = "DUP",
                Description = "Project with duplicate key"
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                duplicateProjectDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var projectsWithKey = await dbContext.Set<Project>()
                    .CountAsync(p => p.Key == "DUP");

                projectsWithKey.Should().Be(1);
            });
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOk()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Project Alpha",
                Key = "PA",
                Description = "Project used for GetById integration test"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // Act
            var response = await Client.GetAsync($"/api/projects/{createdProject!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var project = await response.Content.ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();
            project!.Id.Should().Be(createdProject.Id);
            project.Name.Should().Be(createDto.Name);
            project.Key.Should().Be(createDto.Key);
            project.Description.Should().Be(createDto.Description);
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 : crée le projet
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Private Project",
                Key = "PP",
                Description = "Project inaccessible to non-members"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // User 2 : utilisateur différent, non membre du projet
            await AuthenticateAsync(
                "outsider@test.com",
                "outsider-user");

            // Act
            var response = await Client.GetAsync($"/api/projects/{createdProject!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByKey_WhenUserIsProjectMember_ShouldReturnOk()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Project By Key",
                Key = "PBK",
                Description = "Project used for GetByKey integration test"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var response = await Client.GetAsync($"/api/projects/key/{createDto.Key}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var project = await response.Content.ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();
            project!.Name.Should().Be(createDto.Name);
            project.Key.Should().Be(createDto.Key);
            project.Description.Should().Be(createDto.Description);
        }

        [Fact]
        public async Task GetByKey_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Private Key Project",
                Key = "PKP",
                Description = "Project inaccessible to non-members"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            await AuthenticateAsync(
                "outsider-key@test.com",
                "outsider-key-user");

            // Act
            var response = await Client.GetAsync($"/api/projects/key/{createDto.Key}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByKey_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            // Act
            var response = await Client.GetAsync("/api/projects/key/UNKNOWN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Archive_WhenUserIsManager_ShouldReturnNoContentAndArchiveProject()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Project To Archive",
                Key = "PTA",
                Description = "Project used for archive integration test"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // Act
            var response = await Client.PatchAsync($"/api/projects/{createdProject!.Id}/archive", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().Be(ProjectStatus.Archived);
            });
        }

        [Fact]
        public async Task Archive_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet → Owner + Manager
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Manager Only Project",
                Key = "MOP",
                Description = "Project used to test CanManageProject"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // User 2 se connecte
            await AuthenticateAsync(
                "developer@test.com",
                "developer-user");

            // On ajoute User 2 au projet avec le rôle Developer
            await ExecuteDbContextAsync(async dbContext =>
            {
                var developer = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == "developer@test.com");

                var projectMember = new ProjectMember
                {
                    ProjectId = createdProject!.Id,
                    UserId = developer.Id,
                    Role = ProjectRole.Developer
                };

                dbContext.Set<ProjectMember>().Add(projectMember);

                await dbContext.SaveChangesAsync();
            });

            // Act
            var response = await Client.PatchAsync(
                $"/api/projects/{createdProject!.Id}/archive",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().NotBe(ProjectStatus.Archived);
            });
        }

        [Fact]
        public async Task Archive_WhenProjectIsAlreadyArchived_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Already Archived Project",
                Key = "AAP",
                Description = "Project used to test duplicate archive"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            var firstArchiveResponse = await Client.PatchAsync(
                $"/api/projects/{createdProject!.Id}/archive",
                null);

            firstArchiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.PatchAsync(
                $"/api/projects/{createdProject.Id}/archive",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().Be(ProjectStatus.Archived);
            });
        }

        [Fact]
        public async Task Activate_WhenUserIsManager_ShouldReturnNoContentAndActivateProject()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Project To Activate",
                Key = "PTAC",
                Description = "Project used for activate integration test"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{createdProject!.Id}/archive",
                null);

            archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.PatchAsync(
                $"/api/projects/{createdProject.Id}/activate",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().Be(ProjectStatus.Active);
            });
        }

        [Fact]
        public async Task Activate_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet → Owner + Manager
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Activate Restricted Project",
                Key = "ARP",
                Description = "Project used to test activate authorization"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // Le Manager archive d'abord le projet
            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{createdProject!.Id}/archive",
                null);

            archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // User 2 se connecte
            await AuthenticateAsync(
                "developer-activate@test.com",
                "developer-activate-user");

            // On ajoute User 2 comme Developer du projet
            await ExecuteDbContextAsync(async dbContext =>
            {
                var developer = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == "developer-activate@test.com");

                var projectMember = new ProjectMember
                {
                    ProjectId = createdProject.Id,
                    UserId = developer.Id,
                    Role = ProjectRole.Developer
                };

                dbContext.Set<ProjectMember>().Add(projectMember);

                await dbContext.SaveChangesAsync();
            });

            // Act
            var response = await Client.PatchAsync(
                $"/api/projects/{createdProject.Id}/activate",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().Be(ProjectStatus.Archived);
            });
        }

        [Fact]
        public async Task Activate_WhenProjectIsAlreadyActive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Already Active Project",
                Key = "AACP",
                Description = "Project used to test duplicate activation"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // Act
            var response = await Client.PatchAsync(
                $"/api/projects/{createdProject!.Id}/activate",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Status.Should().Be(ProjectStatus.Active);
            });
        }

        [Fact]
        public async Task ChangeOwner_WhenUserIsCurrentOwner_ShouldReturnNoContentAndChangeOwner()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Ownership Project",
                Key = "OWN",
                Description = "Project used for owner change integration test"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            Guid newOwnerId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var newOwner = new User
                {
                    Email = "new-owner@test.com",
                    Username = "new-owner",
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(newOwner);

                await dbContext.SaveChangesAsync();

                newOwnerId = newOwner.Id;

                var projectMember = new ProjectMember
                {
                    ProjectId = createdProject!.Id,
                    UserId = newOwner.Id,
                    Role = ProjectRole.Manager
                };

                dbContext.Set<ProjectMember>().Add(projectMember);

                await dbContext.SaveChangesAsync();
            });

            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = newOwnerId
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{createdProject!.Id}/owner",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.OwnerId.Should().Be(newOwnerId);
            });
        }

        [Fact]
        public async Task ChangeOwner_WhenUserIsManagerButNotOwner_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet → Owner + Manager
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Owner Restricted Project",
                Key = "ORP",
                Description = "Project used to test owner authorization"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // User 2 se connecte → il devient l'utilisateur courant
            await AuthenticateAsync(
                "manager-nonowner@test.com",
                "manager-nonowner-user");

            Guid newOwnerId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var manager = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == "manager-nonowner@test.com");

                var managerMembership = new ProjectMember
                {
                    ProjectId = createdProject!.Id,
                    UserId = manager.Id,
                    Role = ProjectRole.Manager
                };

                dbContext.Set<ProjectMember>().Add(managerMembership);

                var newOwner = new User
                {
                    Email = "future-owner@test.com",
                    Username = "future-owner",
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(newOwner);

                await dbContext.SaveChangesAsync();

                newOwnerId = newOwner.Id;
            });

            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = newOwnerId
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{createdProject!.Id}/owner",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Project To Delete",
                Key = "PTD",
                Description = "Project used to test Admin-only deletion"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{createdProject!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var projectExists = await dbContext.Set<Project>()
                    .AnyAsync(p => p.Id == createdProject.Id);

                projectExists.Should().BeTrue();
            });
        }

        [Fact]
        public async Task Delete_WhenUserIsAdmin_ShouldReturnNoContentAndDeleteProject()
        {
            // Arrange

            // User normal crée le projet
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Admin Delete Project",
                Key = "ADP",
                Description = "Project used to test Admin deletion"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // On se reconnecte maintenant comme Admin
            await AuthenticateAsync(
                  "admin@test.com",
                  "admin-user",
                  SystemRole.Admin);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/projects/{createdProject!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var projectExists = await dbContext.Set<Project>()
                    .AnyAsync(p => p.Id == createdProject.Id);

                projectExists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Update_WhenUserIsManager_ShouldReturnOkAndUpdateProject()
        {
            // Arrange
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Original Project",
                Key = "UPD",
                Description = "Original description"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            var updateDto = new UpdateProjectDto
            {
                Name = "Updated Project",
                Description = "Updated description"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{createdProject!.Id}",
                updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Id.Should().Be(createdProject.Id);
            result.Name.Should().Be(updateDto.Name);
            result.Description.Should().Be(updateDto.Description);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Name.Should().Be(updateDto.Name);
                project.Description.Should().Be(updateDto.Description);
                project.Key.Should().Be(createDto.Key);
            });
        }

        [Fact]
        public async Task Update_WhenUserIsDeveloper_ShouldReturnForbiddenAndNotUpdateProject()
        {
            // Arrange

            // User 1 crée le projet → Owner + Manager
            await AuthenticateAsync();

            var createDto = new CreateProjectDto
            {
                Name = "Original Project",
                Key = "DEVUPD",
                Description = "Original description"
            };

            var createResponse = await Client.PostAsJsonAsync("/api/projects", createDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

            createdProject.Should().NotBeNull();

            // User 2 devient l'utilisateur courant
            await AuthenticateAsync(
                "developer-update@test.com",
                "developer-update-user");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var developer = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == "developer-update@test.com");

                var projectMember = new ProjectMember
                {
                    ProjectId = createdProject!.Id,
                    UserId = developer.Id,
                    Role = ProjectRole.Developer
                };

                dbContext.Set<ProjectMember>().Add(projectMember);

                await dbContext.SaveChangesAsync();
            });

            var updateDto = new UpdateProjectDto
            {
                Name = "Hacked Project Name",
                Description = "Developer should not be able to update this"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{createdProject!.Id}",
                updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var project = await dbContext.Set<Project>()
                    .SingleAsync(p => p.Id == createdProject.Id);

                project.Name.Should().Be(createDto.Name);
                project.Description.Should().Be(createDto.Description);
            });
        }
    }
}