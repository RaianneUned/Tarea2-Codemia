namespace Tarea2.Models;

public class ProjectCatalogViewModel
{
    public List<ProjectSummary> RecentProjects { get; set; } = new();
    public List<ProjectSummary> AllProjects { get; set; } = new();
}
