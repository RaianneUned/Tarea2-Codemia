using System;
using System.Collections.Generic;

namespace Tarea2.Models;

public class ProjectDetailViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BreadcrumbParent { get; set; } = "Proyectos";
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = new();
    public List<ProjectAttachment> Attachments { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<RatingBreakdownItem> RatingBreakdown { get; set; } = new();
    public List<ProjectReview> Reviews { get; set; } = new();
    public List<ProjectMetric> Metrics { get; set; } = new();
    public bool CanComment { get; set; }
}

public class ProjectAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}

public class RatingBreakdownItem
{
    public int Stars { get; set; }
    public int Percentage { get; set; }
}

public class ProjectReview
{
    public string ReviewerName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
}

public class ProjectMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
