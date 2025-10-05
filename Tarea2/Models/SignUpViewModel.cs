using System.ComponentModel.DataAnnotations;

namespace Tarea2.Models;

public class SignUpViewModel
{
    [Required]
    [Display(Name = "Usuario")]
    [RegularExpression("^[a-zA-Z0-9_.-]{4,32}$", ErrorMessage = "El usuario debe tener entre 4 y 32 caracteres alfanuméricos.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nombre")]
    [StringLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Apellido")]
    [StringLength(64)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}
