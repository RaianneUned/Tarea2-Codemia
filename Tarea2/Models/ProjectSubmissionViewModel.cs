using System.ComponentModel.DataAnnotations;

namespace Tarea2.Models;

public class ProjectSubmissionViewModel
{
    [Required]
    [Display(Name = "Título de la tarea")]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Descripción breve")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Lenguajes de programación utilizados")]
    [StringLength(250)]
    public string Languages { get; set; } = string.Empty;

    [Display(Name = "URL del repositorio")]
    [Url]
    public string? RepositoryUrl { get; set; }

    [Display(Name = "Categoría o tipo de tarea")]
    [Required]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Nivel de dificultad")]
    [Required]
    public string Difficulty { get; set; } = string.Empty;

    public string? AttachmentName { get; set; }
}
