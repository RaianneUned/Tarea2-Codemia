using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tarea2.Models;

public class ProjectCatalogData
{
    [JsonPropertyName("recentSlugs")]
    public List<string> RecentSlugs { get; set; } = new();

    [JsonPropertyName("projects")]
    public List<ProjectData> Projects { get; set; } = new();
}

public class ProjectData
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("technology")]
    public string Technology { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("reviewCount")]
    public int ReviewCount { get; set; }

    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<ProjectAttachmentData> Attachments { get; set; } = new();

    [JsonPropertyName("ratingBreakdown")]
    public List<RatingBreakdownItemData> RatingBreakdown { get; set; } = new();

    [JsonPropertyName("reviews")]
    public List<ProjectReviewData> Reviews { get; set; } = new();

    [JsonPropertyName("metrics")]
    public List<ProjectMetricData> Metrics { get; set; } = new();

    [JsonPropertyName("authorUsername")]
    public string AuthorUsername { get; set; } = string.Empty;

    [JsonPropertyName("authorDisplayName")]
    public string AuthorDisplayName { get; set; } = string.Empty;
}

public class ProjectAttachmentData
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;
}

public class RatingBreakdownItemData
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
}

public class ProjectReviewData
{
    [JsonPropertyName("reviewerName")]
    public string ReviewerName { get; set; } = string.Empty;

    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; set; } = string.Empty;

    [JsonPropertyName("timeAgo")]
    public string TimeAgo { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("upVotes")]
    public int UpVotes { get; set; }

    [JsonPropertyName("downVotes")]
    public int DownVotes { get; set; }
}

public class ProjectMetricData
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
