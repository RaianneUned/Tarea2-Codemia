using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tarea2.Models;
using Tarea2.Services;

namespace Tarea2.Controllers;

public class ProjectsController : Controller
{
    private readonly IUserSessionService _sessionService;
    private readonly IProjectCatalogService _catalogService;
    private static readonly TextInfo SpanishTextInfo = new CultureInfo("es-ES").TextInfo;

    public ProjectsController(IUserSessionService sessionService, IProjectCatalogService catalogService)
    {
        _sessionService = sessionService;
        _catalogService = catalogService;
    }

    public IActionResult Mine()
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Index", "Home");
        }

        var username = _sessionService.GetCurrentUsername(HttpContext) ?? string.Empty;
        var displayName = _sessionService.GetCurrentDisplayName(HttpContext) ?? username;
        var allProjects = _catalogService.GetProjectsByAuthor(username).ToList();
        allProjects.Reverse();
        var recentProjects = _catalogService.GetRecentProjects()
            .Where(p => string.Equals(p.AuthorUsername, username, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (recentProjects.Count < 5)
        {
            var supplementalProjects = allProjects
                .Where(p => !recentProjects.Any(r => string.Equals(r.Slug, p.Slug, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.ReviewCount)
                .ThenByDescending(p => p.Rating)
                .Take(5 - recentProjects.Count)
                .ToList();

            recentProjects.AddRange(supplementalProjects);
        }

        var totalReviews = allProjects.Sum(p => p.ReviewCount);
        var ratedProjects = allProjects.Where(p => p.ReviewCount > 0 && p.Rating > 0).ToList();
        var weightedAverage = ratedProjects.Count > 0
            ? Math.Round(ratedProjects.Sum(p => p.Rating * p.ReviewCount) / ratedProjects.Sum(p => p.ReviewCount), 2)
            : 0;

        var stats = new MyProjectsStats
        {
            ProjectCount = allProjects.Count,
            TotalReviews = totalReviews,
            AverageRating = weightedAverage,
            RatedProjectCount = ratedProjects.Count,
            AverageReviewsPerProject = allProjects.Count > 0 ? Math.Round((double)totalReviews / allProjects.Count, 1) : 0
        };

        var projectSections = new List<MyProjectReviewSection>();

        foreach (var project in allProjects)
        {
            var detail = _catalogService.GetProjectDetail(project.Slug);
            var languages = ResolveLanguages(project, detail);

            project.Languages = languages;
            project.TaskType = DetermineTaskType(project, detail);
            project.Difficulty = DetermineDifficulty(project, detail);

            var reviews = (detail?.Reviews ?? new List<ProjectReview>())
                .Select(review => new ProjectReviewDisplay
                {
                    ReviewerName = review.ReviewerName,
                    AvatarUrl = review.AvatarUrl,
                    TimeAgo = review.TimeAgo,
                    CreatedAt = review.CreatedAt,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    UpVotes = review.UpVotes,
                    DownVotes = review.DownVotes
                })
                .OrderByDescending(r => r.HelpfulScore)
                .ThenByDescending(r => r.CreatedAt ?? DateTime.MinValue)
                .ToList();

            projectSections.Add(new MyProjectReviewSection
            {
                Project = project,
                Reviews = reviews,
                Languages = languages
            });
        }

        var orderedSections = projectSections;

        var filterOptions = new MyProjectsFilterOptions
        {
            TaskTypes = orderedSections
                .Select(p => p.Project.TaskType)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Languages = orderedSections
                .SelectMany(p => p.Languages)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Difficulties = orderedSections
                .Select(p => p.Project.Difficulty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        var model = new MyProjectsViewModel
        {
            AuthorDisplayName = displayName,
            RecentProjects = recentProjects,
            AllProjects = orderedSections.Select(p => p.Project).ToList(),
            ProjectsWithReviews = orderedSections,
            Filters = filterOptions,
            Stats = stats
        };

        ViewData["Title"] = "Mis Proyectos";
        ViewData["UseFullWidthLayout"] = true;
        ViewData["BodyClass"] = "font-display bg-background-light text-gray-800";
        return View(model);
    }

    private static string DetermineTaskType(ProjectSummary summary, ProjectDetailViewModel? detail)
    {
        var provided = detail?.TaskType ?? summary.TaskType;
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return NormalizeLabel(provided);
        }

        var text = $"{summary.Title} {summary.Description}".ToLowerInvariant();

        if (text.Contains("interfaz", StringComparison.OrdinalIgnoreCase) || text.Contains("diseño", StringComparison.OrdinalIgnoreCase) || text.Contains("ux", StringComparison.OrdinalIgnoreCase))
        {
            return "Diseño UI";
        }

        if (text.Contains("algoritmo", StringComparison.OrdinalIgnoreCase) || text.Contains("ordenamiento", StringComparison.OrdinalIgnoreCase) || text.Contains("optimización", StringComparison.OrdinalIgnoreCase) || text.Contains("optimizacion", StringComparison.OrdinalIgnoreCase))
        {
            return "Algoritmos";
        }

        if (text.Contains("simul", StringComparison.OrdinalIgnoreCase))
        {
            return "Simulación";
        }

        if (text.Contains("modelo", StringComparison.OrdinalIgnoreCase) || text.Contains("datos", StringComparison.OrdinalIgnoreCase) || text.Contains("predic", StringComparison.OrdinalIgnoreCase) || text.Contains("anal", StringComparison.OrdinalIgnoreCase))
        {
            return "Ciencia de Datos";
        }

        if (text.Contains("seguridad", StringComparison.OrdinalIgnoreCase) || text.Contains("encript", StringComparison.OrdinalIgnoreCase))
        {
            return "Seguridad";
        }

        if (text.Contains("móvil", StringComparison.OrdinalIgnoreCase) || text.Contains("movil", StringComparison.OrdinalIgnoreCase))
        {
            return "Aplicaciones Móviles";
        }

        if (text.Contains("api", StringComparison.OrdinalIgnoreCase) || text.Contains("servicio", StringComparison.OrdinalIgnoreCase) || text.Contains("backend", StringComparison.OrdinalIgnoreCase))
        {
            return "Backend";
        }

        if (text.Contains("robot", StringComparison.OrdinalIgnoreCase) || text.Contains("sensor", StringComparison.OrdinalIgnoreCase) || text.Contains("iot", StringComparison.OrdinalIgnoreCase))
        {
            return "IoT";
        }

        return "General";
    }

    private static string DetermineDifficulty(ProjectSummary summary, ProjectDetailViewModel? detail)
    {
        var provided = detail?.Difficulty ?? summary.Difficulty;
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return NormalizeLabel(provided);
        }

        if (summary.ReviewCount >= 5 || summary.Rating >= 4.5)
        {
            return "Avanzado";
        }

        if (summary.ReviewCount >= 2 || summary.Rating >= 3.5)
        {
            return "Intermedio";
        }

        return "Principiante";
    }

    private static List<string> ResolveLanguages(ProjectSummary summary, ProjectDetailViewModel? detail)
    {
        var sourceLanguages = detail?.Languages ?? summary.Languages ?? new List<string>();

        var normalized = sourceLanguages
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeLabel)
            .ToList();

        if (normalized.Count == 0 && !string.IsNullOrWhiteSpace(summary.Technology))
        {
            normalized = summary.Technology
                .Split(new[] { ',', '/', '|', '+', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeLabel)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }

        return normalized
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.Trim().ToLowerInvariant();
        return SpanishTextInfo.ToTitleCase(lower);
    }

    public IActionResult Upload()
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["Title"] = "Subir Proyecto";
        ViewData["UseFullWidthLayout"] = true;
        ViewData["BodyClass"] = "font-display bg-background-light text-gray-800";
        ViewData["CurrentUsername"] = _sessionService.GetCurrentUsername(HttpContext) ?? string.Empty;
        return View(new ProjectSubmissionViewModel());
    }

    [HttpGet]
    public IActionResult Edit(string slug)
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return RedirectToAction(nameof(Mine));
        }

        var username = _sessionService.GetCurrentUsername(HttpContext) ?? string.Empty;
        var project = _catalogService.GetProjectBySlug(slug);

        if (project is null || !string.Equals(project.AuthorUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var model = new ProjectEditViewModel
        {
            Slug = project.Slug,
            Title = project.Title,
            Description = project.Description,
            Overview = project.Overview,
            Technology = project.Technology,
            Rating = project.Rating,
            ReviewCount = project.ReviewCount
        };

        ViewData["Title"] = "Editar Proyecto";
        ViewData["UseFullWidthLayout"] = true;
        ViewData["BodyClass"] = "font-display bg-background-light text-gray-800";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ProjectEditViewModel model)
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar Proyecto";
            ViewData["UseFullWidthLayout"] = true;
            ViewData["BodyClass"] = "font-display bg-background-light text-gray-800";
            return View(model);
        }

        var username = _sessionService.GetCurrentUsername(HttpContext) ?? string.Empty;
        var project = _catalogService.GetProjectBySlug(model.Slug);

        if (project is null || !string.Equals(project.AuthorUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        project.Title = model.Title.Trim();
        project.Description = model.Description.Trim();
        project.Overview = string.IsNullOrWhiteSpace(model.Overview) ? model.Description.Trim() : model.Overview.Trim();
        project.Technology = model.Technology.Trim();

        var updated = _catalogService.UpdateProject(project);

        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "No se pudo actualizar el proyecto. Inténtalo nuevamente.");
            ViewData["Title"] = "Editar Proyecto";
            ViewData["UseFullWidthLayout"] = true;
            ViewData["BodyClass"] = "font-display bg-background-light text-gray-800";
            return View(model);
        }

        TempData["StatusMessage"] = "Proyecto actualizado correctamente.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string slug)
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            TempData["StatusMessage"] = "No se pudo identificar el proyecto a eliminar.";
            return RedirectToAction(nameof(Mine));
        }

        var username = _sessionService.GetCurrentUsername(HttpContext) ?? string.Empty;
        var project = _catalogService.GetProjectBySlug(slug);

        if (project is null || !string.Equals(project.AuthorUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var deleted = _catalogService.DeleteProject(slug);
        TempData["StatusMessage"] = deleted
            ? "Proyecto eliminado correctamente."
            : "No se pudo eliminar el proyecto. Inténtalo nuevamente.";

        return RedirectToAction(nameof(Mine));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upload(ProjectSubmissionViewModel model)
    {
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return Unauthorized(new { success = false, message = "Tu sesión ha expirado, vuelve a iniciar sesión." });
        }

        if (!HasRepositoryOrAttachment(model, Request.Form.Files))
        {
            ModelState.AddModelError("RepositoryOrAttachment", "Proporciona al menos un enlace al repositorio o adjunta un archivo.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, errors = CollectErrors(ModelState) });
        }

        var username = _sessionService.GetCurrentUsername(HttpContext) ?? "usuario";
        var displayName = _sessionService.GetCurrentDisplayName(HttpContext) ?? username;

        var languagesList = ParseLanguagesInput(model.Languages);
        var technologySummary = languagesList.Count > 0
            ? string.Join(" · ", languagesList)
            : model.Languages.Trim();

        var taskType = NormalizeLabel(model.Category);
        var difficulty = NormalizeLabel(model.Difficulty);

        var project = new ProjectData
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Overview = model.Description.Trim(),
            Technology = technologySummary,
            TaskType = taskType,
            Difficulty = difficulty,
            Languages = languagesList,
            AuthorUsername = username,
            AuthorDisplayName = displayName,
            Rating = 0,
            ReviewCount = 0,
            ImageUrl = "https://via.placeholder.com/400x300?text=Proyecto",
            RatingBreakdown = new List<RatingBreakdownItemData>
            {
                new() { Stars = 5, Percentage = 0 },
                new() { Stars = 4, Percentage = 0 },
                new() { Stars = 3, Percentage = 0 },
                new() { Stars = 2, Percentage = 0 },
                new() { Stars = 1, Percentage = 0 }
            },
            Metrics = new List<ProjectMetricData>
            {
                new() { Label = "Calificación Promedio", Value = "0.0" },
                new() { Label = "Número de Revisiones", Value = "0" }
            }
        };

        var attachments = new List<ProjectAttachmentData>();
        foreach (var file in Request.Form.Files)
        {
            if (file.Length <= 0)
            {
                continue;
            }

            attachments.Add(new ProjectAttachmentData
            {
                FileName = file.FileName,
                FileType = file.ContentType,
                DownloadUrl = string.Empty
            });
        }

        if (attachments.Count > 0)
        {
            project.Attachments = attachments;
        }

        var addedProject = _catalogService.AddProject(project);

        return Json(new
        {
            success = true,
            message = "Tu proyecto se guardó correctamente.",
            projectSlug = addedProject.Slug
        });
    }

    private static List<string> ParseLanguagesInput(string languages)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(languages))
        {
            return result;
        }

        var separators = new[] { ',', ';', '/', '|', '\\', '\r', '\n' };
        var fragments = languages.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var fragment in fragments)
        {
            var value = fragment.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var exists = result.Exists(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static bool HasRepositoryOrAttachment(ProjectSubmissionViewModel model, IFormFileCollection files)
    {
        var hasRepo = !string.IsNullOrWhiteSpace(model.RepositoryUrl);
        var hasAttachment = files != null && files.Count > 0 && HasNonEmptyFile(files);
        return hasRepo || hasAttachment;
    }

    private static bool HasNonEmptyFile(IFormFileCollection files)
    {
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, string[]> CollectErrors(ModelStateDictionary modelState)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in modelState)
        {
            if (entry.Value.Errors.Count == 0)
            {
                continue;
            }

            var messages = new List<string>();
            foreach (var error in entry.Value.Errors)
            {
                if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
                {
                    messages.Add(error.ErrorMessage);
                }
            }

            if (messages.Count > 0)
            {
                result[entry.Key] = messages.ToArray();
            }
        }

        return result;
    }
}
