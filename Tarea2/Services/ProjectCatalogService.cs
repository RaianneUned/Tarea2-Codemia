using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Tarea2.Models;

namespace Tarea2.Services;

public interface IProjectCatalogService
{
    IReadOnlyList<ProjectSummary> GetAllProjects();
    IReadOnlyList<ProjectSummary> GetRecentProjects();
    ProjectDetailViewModel? GetProjectDetail(string slug);
    IReadOnlyList<ProjectSummary> GetProjectsByAuthor(string username);
    ProjectSummary AddProject(ProjectData project);
    ProjectData? GetProjectBySlug(string slug);
    bool UpdateProject(ProjectData updatedProject);
    bool DeleteProject(string slug);
    ProjectReviewResult AddReview(ProjectReviewInput input);
}

public class ProjectCatalogService : IProjectCatalogService
{
    private readonly ProjectCatalogData _catalog;
    private readonly Dictionary<string, ProjectData> _projectsBySlug;
    private readonly string _dataPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly object _lock = new();

    public ProjectCatalogService(IHostEnvironment environment)
    {
        _dataPath = Path.Combine(environment.ContentRootPath, "Data", "projects.json");
        if (!File.Exists(_dataPath))
        {
            throw new FileNotFoundException($"No se encontró el archivo de datos en {_dataPath}");
        }

        using var stream = File.OpenRead(_dataPath);
        _catalog = JsonSerializer.Deserialize<ProjectCatalogData>(stream, _jsonOptions)
                   ?? throw new InvalidOperationException("No se pudo deserializar el catálogo de proyectos.");

        _projectsBySlug = _catalog.Projects.ToDictionary(p => p.Slug, p => p, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ProjectSummary> GetAllProjects() =>
        _catalog.Projects.Select(ToSummary).ToList();

    public IReadOnlyList<ProjectSummary> GetRecentProjects() =>
        _catalog.RecentSlugs
            .Select(slug => _projectsBySlug.TryGetValue(slug, out var project) ? project : null)
            .Where(project => project is not null)
            .Select(project => ToSummary(project!))
            .ToList();

    public ProjectDetailViewModel? GetProjectDetail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return _projectsBySlug.TryGetValue(slug, out var project)
            ? ToDetail(project)
            : null;
    }

    public IReadOnlyList<ProjectSummary> GetProjectsByAuthor(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Array.Empty<ProjectSummary>();
        }

        return _catalog.Projects
            .Where(p => string.Equals(p.AuthorUsername, username, StringComparison.OrdinalIgnoreCase))
            .Select(ToSummary)
            .ToList();
    }

    public ProjectSummary AddProject(ProjectData project)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        lock (_lock)
        {
            project.Slug = EnsureUniqueSlug(string.IsNullOrWhiteSpace(project.Slug) ? project.Title : project.Slug);

            _catalog.Projects.Add(project);
            _projectsBySlug[project.Slug] = project;

            if (!_catalog.RecentSlugs.Contains(project.Slug, StringComparer.OrdinalIgnoreCase))
            {
                _catalog.RecentSlugs.Insert(0, project.Slug);
            }

            SaveCatalog();
            return ToSummary(project);
        }
    }

    public ProjectData? GetProjectBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return _projectsBySlug.TryGetValue(slug, out var project) ? CloneProject(project) : null;
    }

    public bool UpdateProject(ProjectData updatedProject)
    {
        if (updatedProject is null)
        {
            throw new ArgumentNullException(nameof(updatedProject));
        }

        lock (_lock)
        {
            if (!_projectsBySlug.TryGetValue(updatedProject.Slug, out var existing))
            {
                return false;
            }

            existing.Title = updatedProject.Title;
            existing.Description = updatedProject.Description;
            existing.Overview = updatedProject.Overview;
            existing.Technology = updatedProject.Technology;
            existing.ImageUrl = string.IsNullOrWhiteSpace(updatedProject.ImageUrl) ? existing.ImageUrl : updatedProject.ImageUrl;

            _projectsBySlug[existing.Slug] = existing;
            SaveCatalog();
            return true;
        }
    }

    public bool DeleteProject(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        lock (_lock)
        {
            if (!_projectsBySlug.TryGetValue(slug, out var project))
            {
                return false;
            }

            _projectsBySlug.Remove(slug);
            _catalog.Projects.Remove(project);
            _catalog.RecentSlugs.RemoveAll(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase));
            SaveCatalog();
            return true;
        }
    }

    public ProjectReviewResult AddReview(ProjectReviewInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Slug))
        {
            throw new ArgumentException("Slug requerido", nameof(input.Slug));
        }

        lock (_lock)
        {
            if (!_projectsBySlug.TryGetValue(input.Slug, out var project))
            {
                throw new InvalidOperationException("Proyecto no encontrado.");
            }

            input.Rating = Math.Clamp(input.Rating, 1, 5);
            var sanitizedComment = input.Comment?.Trim() ?? string.Empty;

            var reviewerName = string.IsNullOrWhiteSpace(input.ReviewerName)
                ? "Usuario"
                : input.ReviewerName.Trim();
            var avatarSeed = Uri.EscapeDataString(reviewerName);

            var review = new ProjectReviewData
            {
                ReviewerName = reviewerName,
                AvatarUrl = string.IsNullOrWhiteSpace(input.AvatarUrl)
                    ? $"https://api.dicebear.com/7.x/initials/svg?seed={avatarSeed}&backgroundType=gradientLinear&backgroundColor=fb923c,facc15"
                    : input.AvatarUrl,
                TimeAgo = DateTime.UtcNow.ToString("o"),
                Rating = input.Rating,
                Comment = sanitizedComment,
                UpVotes = 0,
                DownVotes = 0
            };

            project.Reviews ??= new List<ProjectReviewData>();
            project.Reviews.Insert(0, review);

            project.ReviewCount = project.Reviews.Count;
            project.Rating = Math.Round(project.Reviews.Average(r => r.Rating), 2);

            UpdateRatingBreakdown(project);
            UpdateMetrics(project);

            SaveCatalog();

            var detail = ToDetail(project);

            return new ProjectReviewResult
            {
                AverageRating = detail.AverageRating,
                ReviewCount = detail.ReviewCount,
                RatingBreakdown = detail.RatingBreakdown,
                Reviews = detail.Reviews,
                Metrics = detail.Metrics
            };
        }
    }

    private static ProjectSummary ToSummary(ProjectData data) => new()
    {
        Slug = data.Slug,
        Title = data.Title,
        Description = data.Description,
        ImageUrl = data.ImageUrl,
        Technology = data.Technology,
        TaskType = data.TaskType,
        Difficulty = data.Difficulty,
        Languages = (data.Languages ?? new List<string>()).ToList(),
        Rating = data.Rating,
        ReviewCount = data.ReviewCount,
        AuthorUsername = data.AuthorUsername,
        AuthorDisplayName = data.AuthorDisplayName
    };

    private static ProjectDetailViewModel ToDetail(ProjectData data)
    {
        var detail = new ProjectDetailViewModel
        {
            Slug = data.Slug,
            Title = data.Title,
            Overview = string.IsNullOrWhiteSpace(data.Overview) ? data.Description : data.Overview,
            Description = data.Description,
            BreadcrumbParent = "Proyectos",
            AuthorUsername = data.AuthorUsername,
            AuthorDisplayName = data.AuthorDisplayName,
            ImageUrl = data.ImageUrl,
            Technology = data.Technology,
            TaskType = data.TaskType,
            Difficulty = data.Difficulty,
            Languages = (data.Languages ?? new List<string>()).ToList(),
            AverageRating = data.Rating,
            ReviewCount = data.ReviewCount,
            Attachments = data.Attachments?.Select(a => new ProjectAttachment
            {
                FileName = a.FileName,
                FileType = a.FileType,
                DownloadUrl = a.DownloadUrl
            }).ToList() ?? new List<ProjectAttachment>(),
            RatingBreakdown = data.RatingBreakdown?.Select(r => new RatingBreakdownItem
            {
                Stars = r.Stars,
                Percentage = r.Percentage
            }).ToList() ?? new List<RatingBreakdownItem>(),
            Reviews = data.Reviews?.Select(r =>
            {
                var createdAt = ParseReviewTimestamp(r.TimeAgo);
                return new ProjectReview
                {
                    ReviewerName = r.ReviewerName,
                    AvatarUrl = r.AvatarUrl,
                    CreatedAt = createdAt,
                    TimeAgo = FormatRelativeTime(createdAt, r.TimeAgo),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    UpVotes = r.UpVotes,
                    DownVotes = r.DownVotes
                };
            }).ToList() ?? new List<ProjectReview>(),
            Metrics = data.Metrics?.Select(m => new ProjectMetric
            {
                Label = m.Label,
                Value = m.Value
            }).ToList() ?? new List<ProjectMetric>()
        };

        if (!detail.Metrics.Any())
        {
            detail.Metrics.Add(new ProjectMetric
            {
                Label = "Calificación Promedio",
                Value = detail.AverageRating.ToString("0.0")
            });
            detail.Metrics.Add(new ProjectMetric
            {
                Label = "Número de Revisiones",
                Value = detail.ReviewCount.ToString()
            });
        }

        return detail;
    }

    private static ProjectData CloneProject(ProjectData source)
    {
        return new ProjectData
        {
            Slug = source.Slug,
            Title = source.Title,
            Description = source.Description,
            ImageUrl = source.ImageUrl,
            Technology = source.Technology,
            TaskType = source.TaskType,
            Difficulty = source.Difficulty,
            Languages = (source.Languages ?? new List<string>()).ToList(),
            Rating = source.Rating,
            ReviewCount = source.ReviewCount,
            Overview = source.Overview,
            Attachments = (source.Attachments ?? new List<ProjectAttachmentData>()).Select(a => new ProjectAttachmentData
            {
                FileName = a.FileName,
                FileType = a.FileType,
                DownloadUrl = a.DownloadUrl
            }).ToList(),
            RatingBreakdown = (source.RatingBreakdown ?? new List<RatingBreakdownItemData>()).Select(r => new RatingBreakdownItemData
            {
                Stars = r.Stars,
                Percentage = r.Percentage
            }).ToList(),
            Reviews = (source.Reviews ?? new List<ProjectReviewData>()).Select(r => new ProjectReviewData
            {
                ReviewerName = r.ReviewerName,
                AvatarUrl = r.AvatarUrl,
                TimeAgo = r.TimeAgo,
                Rating = r.Rating,
                Comment = r.Comment,
                UpVotes = r.UpVotes,
                DownVotes = r.DownVotes
            }).ToList(),
            Metrics = (source.Metrics ?? new List<ProjectMetricData>()).Select(m => new ProjectMetricData
            {
                Label = m.Label,
                Value = m.Value
            }).ToList(),
            AuthorUsername = source.AuthorUsername,
            AuthorDisplayName = source.AuthorDisplayName
        };
    }

    private static void UpdateRatingBreakdown(ProjectData project)
    {
        var buckets = new Dictionary<int, int> { {1,0}, {2,0}, {3,0}, {4,0}, {5,0} };
        foreach (var review in project.Reviews ?? Enumerable.Empty<ProjectReviewData>())
        {
            var key = (int)Math.Round(Math.Clamp(review.Rating, 1, 5));
            buckets[key] += 1;
        }

        var total = project.Reviews?.Count ?? 0;
        project.RatingBreakdown = buckets
            .OrderByDescending(kvp => kvp.Key)
            .Select(kvp => new RatingBreakdownItemData
            {
                Stars = kvp.Key,
                Percentage = total == 0 ? 0 : (int)Math.Round(kvp.Value * 100.0 / total)
            })
            .ToList();
    }

    private static void UpdateMetrics(ProjectData project)
    {
        project.Metrics ??= new List<ProjectMetricData>();

        void UpsertMetric(string label, string value)
        {
            var metric = project.Metrics.FirstOrDefault(m => string.Equals(m.Label, label, StringComparison.OrdinalIgnoreCase));
            if (metric is null)
            {
                project.Metrics.Add(new ProjectMetricData { Label = label, Value = value });
            }
            else
            {
                metric.Value = value;
            }
        }

        UpsertMetric("Calificación Promedio", project.Rating.ToString("0.0"));
        UpsertMetric("Número de Revisiones", project.ReviewCount.ToString());
    }

    private static DateTime? ParseReviewTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        var match = Regex.Match(value, @"Hace\s+(\d+)\s+([\p{L}]+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var amount))
        {
            var unit = NormalizeUnit(match.Groups[2].Value);
            var now = DateTime.UtcNow;

            return unit switch
            {
                var u when u.StartsWith("minut", StringComparison.Ordinal) => now - TimeSpan.FromMinutes(amount),
                var u when u.StartsWith("hora", StringComparison.Ordinal) => now - TimeSpan.FromHours(amount),
                var u when u.StartsWith("dia", StringComparison.Ordinal) => now - TimeSpan.FromDays(amount),
                var u when u.StartsWith("semana", StringComparison.Ordinal) => now - TimeSpan.FromDays(amount * 7),
                var u when u.StartsWith("mes", StringComparison.Ordinal) => now - TimeSpan.FromDays(amount * 30),
                var u when u.StartsWith("ano", StringComparison.Ordinal) => now - TimeSpan.FromDays(amount * 365),
                _ => now
            };
        }

        return null;
    }

    private static string FormatRelativeTime(DateTime? timestamp, string fallback = "")
    {
        if (timestamp is null)
        {
            return string.IsNullOrWhiteSpace(fallback) ? "Hace unos segundos" : fallback;
        }

        var now = DateTime.UtcNow;
        var difference = now - timestamp.Value;

        if (difference.TotalSeconds < 0)
        {
            difference = TimeSpan.Zero;
        }

        static string FormatUnit(int value, string singular, string plural) =>
            value == 1 ? $"Hace 1 {singular}" : $"Hace {value} {plural}";

        if (difference.TotalMinutes < 1)
        {
            return "Hace unos segundos";
        }

        if (difference.TotalHours < 1)
        {
            var minutes = Math.Max(1, (int)Math.Floor(difference.TotalMinutes));
            return FormatUnit(minutes, "minuto", "minutos");
        }

        if (difference.TotalDays < 1)
        {
            var hours = Math.Max(1, (int)Math.Floor(difference.TotalHours));
            return FormatUnit(hours, "hora", "horas");
        }

        if (difference.TotalDays < 7)
        {
            var days = Math.Max(1, (int)Math.Floor(difference.TotalDays));
            return FormatUnit(days, "día", "días");
        }

        if (difference.TotalDays < 30)
        {
            var weeks = Math.Max(1, (int)Math.Floor(difference.TotalDays / 7));
            return FormatUnit(weeks, "semana", "semanas");
        }

        if (difference.TotalDays < 365)
        {
            var months = Math.Max(1, (int)Math.Floor(difference.TotalDays / 30));
            return FormatUnit(months, "mes", "meses");
        }

        var years = Math.Max(1, (int)Math.Floor(difference.TotalDays / 365));
        return FormatUnit(years, "año", "años");
    }

    private static string NormalizeUnit(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input
            .ToLowerInvariant()
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal)
            .Replace("ñ", "n", StringComparison.Ordinal);
    }

    private string EnsureUniqueSlug(string source)
    {
        var baseSlug = GenerateSlug(source);
        var slug = baseSlug;
        var index = 1;

        while (_projectsBySlug.ContainsKey(slug))
        {
            slug = $"{baseSlug}-{index++}";
        }

        return slug;
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Guid.NewGuid().ToString("n");
        }

        var slug = new string(text.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("n") : slug;
    }

    private void SaveCatalog()
    {
        var json = JsonSerializer.Serialize(_catalog, _jsonOptions);
        File.WriteAllText(_dataPath, json);
    }
}
