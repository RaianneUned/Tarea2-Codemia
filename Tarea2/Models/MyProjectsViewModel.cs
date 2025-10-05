using System;
using System.Collections.Generic;
using System.Linq;

namespace Tarea2.Models;

public class MyProjectsViewModel
{
    public string AuthorDisplayName { get; set; } = string.Empty;

    public IReadOnlyList<ProjectSummary> RecentProjects { get; set; } = new List<ProjectSummary>();

    public IReadOnlyList<ProjectSummary> AllProjects { get; set; } = new List<ProjectSummary>();

    public IReadOnlyList<MyProjectReviewSection> ProjectsWithReviews { get; set; } = new List<MyProjectReviewSection>();

    public MyProjectsFilterOptions Filters { get; set; } = new();

    public MyProjectsStats Stats { get; set; } = new();
}

public class MyProjectsStats
{
    public int ProjectCount { get; set; }

    public int TotalReviews { get; set; }

    public double AverageRating { get; set; }

    public int RatedProjectCount { get; set; }

    public double AverageReviewsPerProject { get; set; }

    public int UnratedProjectCount => ProjectCount - RatedProjectCount;
}

public class MyProjectsFilterOptions
{
    public IReadOnlyList<string> TaskTypes { get; set; } = new List<string>();
    public IReadOnlyList<string> Languages { get; set; } = new List<string>();
    public IReadOnlyList<string> Difficulties { get; set; } = new List<string>();
}

public class MyProjectReviewSection
{
    public ProjectSummary Project { get; set; } = new();

    public IReadOnlyList<ProjectReviewDisplay> Reviews { get; set; } = new List<ProjectReviewDisplay>();

    public IReadOnlyList<string> Languages { get; set; } = new List<string>();

    public string DisplayTechnology => Languages.Any() ? string.Join(", ", Languages) : Project.Technology;

    public int ReviewCount => Reviews.Count;
}

public class ProjectReviewDisplay
{
    public string ReviewerName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int HelpfulScore => UpVotes - DownVotes;
}
