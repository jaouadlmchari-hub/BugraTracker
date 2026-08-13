using BugTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;
namespace BugTracker.Application.DTOs.ProjectMembers;
public class AddProjectMemberDto 
{ 
  
    public Guid UserId { get; set; } 
    public ProjectRole Role { get; set; } = ProjectRole.Developer;
}