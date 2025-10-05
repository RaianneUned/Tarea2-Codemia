using System.ComponentModel.DataAnnotations;

namespace Tarea2.Models;

public class ProjectEditViewModel
{
    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Título")]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Descripción breve")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Resumen detallado")]
    [StringLength(2000)]
    public string Overview { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tecnologías / Lenguajes")]
    [StringLength(250)]
    public string Technology { get; set; } = string.Empty;

    public double Rating { get; set; }

    public int ReviewCount { get; set; }
}
