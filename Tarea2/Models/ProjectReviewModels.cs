using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tarea2.Models;

public class ProjectReviewInput
{
    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public double Rating { get; set; }

    [Required]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;

    [StringLength(120)]
    public string ReviewerName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;
}

public class ProjectReviewResult
{
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public IReadOnlyList<RatingBreakdownItem> RatingBreakdown { get; set; } = new List<RatingBreakdownItem>();
    public IReadOnlyList<ProjectReview> Reviews { get; set; } = new List<ProjectReview>();
    public IReadOnlyList<ProjectMetric> Metrics { get; set; } = new List<ProjectMetric>();
}
