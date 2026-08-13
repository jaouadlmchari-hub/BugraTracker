using System.ComponentModel.DataAnnotations;

namespace BugTracker.Application.DTOs.Projects;

public class UpdateProjectDto
{
    [Required(ErrorMessage = "Le nom du projet est obligatoire.")]
    [StringLength(100, ErrorMessage = "Le nom du projet ne peut pas dépasser 100 caractères.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
    public string? Description { get; set; }
}