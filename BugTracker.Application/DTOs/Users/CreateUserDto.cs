using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace BugTracker.Application.DTOs.Users;

public class CreateUserDto
{
    [Required(ErrorMessage = "L'adresse email est obligatoire.")]
    [EmailAddress(ErrorMessage = "Le format de l'adresse email est invalide.")]
    [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom complet est obligatoire.")]
    [StringLength(100, ErrorMessage = "Le nom complet ne peut pas dépasser 100 caractères.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
    public string Password { get; set; } = string.Empty;

    [Url(ErrorMessage = "L'URL de l'avatar est invalide.")]
    public string? AvatarUrl { get; set; }
}
