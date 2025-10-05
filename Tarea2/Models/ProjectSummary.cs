using System.Collections.Generic;

namespace Tarea2.Models;

public class ProjectSummary
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = new();
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
}
