using System.ComponentModel.DataAnnotations;

namespace BugTracker.Application.DTOs.Users;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Le mot de passe actuel est obligatoire.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
    [MinLength(8, ErrorMessage = "Le nouveau mot de passe doit contenir au moins 8 caractères.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Le nouveau mot de passe et sa confirmation ne correspondent pas.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}