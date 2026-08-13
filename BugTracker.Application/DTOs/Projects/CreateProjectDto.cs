using System.ComponentModel.DataAnnotations;

namespace BugTracker.Application.DTOs.Projects;

public class CreateProjectDto
{
    [Required(ErrorMessage = "Le nom du projet est obligatoire.")]
    [StringLength(100, ErrorMessage = "Le nom du projet ne peut pas dépasser 100 caractères.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clé du projet est obligatoire.")]
    [StringLength(10, MinimumLength = 2,ErrorMessage = "La clé doit contenir entre 2 et 10 caractères.")]
    public string Key { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
    public string? Description { get; set; }
}