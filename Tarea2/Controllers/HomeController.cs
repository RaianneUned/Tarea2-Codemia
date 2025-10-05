using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tarea2.Models;
using Tarea2.Services;

namespace Tarea2.Controllers;

public class HomeController : Controller
{
    private readonly IProjectCatalogService _catalog;
    private readonly IUserSessionService _sessionService;

    public HomeController(IProjectCatalogService catalog, IUserSessionService sessionService)
    {
        _catalog = catalog;
        _sessionService = sessionService;
    }

    public IActionResult Index()
    {
        var isAuthenticated = _sessionService.IsAuthenticated(HttpContext);

        var model = new ProjectCatalogViewModel
        {
            RecentProjects = isAuthenticated ? _catalog.GetRecentProjects().ToList() : new List<ProjectSummary>(),
            AllProjects = _catalog.GetAllProjects().ToList()
        };

        ViewData["IsAuthenticated"] = isAuthenticated;

        if (isAuthenticated)
        {
            var recentFromSession = GetRecentProjectsFromSession();
            if (recentFromSession.Count > 0)
            {
                model.RecentProjects = recentFromSession.ToList();
            }
        }

        return View(model);
    }

    public IActionResult Author(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return RedirectToAction(nameof(Index));
        }

        var projects = _catalog.GetProjectsByAuthor(username).ToList();
        var isAuthenticated = _sessionService.IsAuthenticated(HttpContext);
        var model = new ProjectCatalogViewModel
        {
            RecentProjects = projects.Take(4).ToList(),
            AllProjects = projects
        };

        var displayName = projects.FirstOrDefault()?.AuthorDisplayName ?? username;
        ViewData["FilteredUser"] = username;
        ViewData["FilteredUserDisplayName"] = displayName;
        ViewData["Title"] = $"Proyectos de {displayName}";
        ViewData["IsAuthenticated"] = isAuthenticated;

        return View("Index", model);
    }

    private IReadOnlyList<ProjectSummary> GetRecentProjectsFromSession()
    {
        var stored = HttpContext.Session.GetString(SessionConstants.RecentProjects);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Array.Empty<ProjectSummary>();
        }

        List<string>? slugs;
        try
        {
            slugs = JsonSerializer.Deserialize<List<string>>(stored);
        }
        catch (JsonException)
        {
            return Array.Empty<ProjectSummary>();
        }

        if (slugs is null || slugs.Count == 0)
        {
            return Array.Empty<ProjectSummary>();
        }

        var summaries = new List<ProjectSummary>();
        foreach (var slug in slugs)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            var detail = _catalog.GetProjectDetail(slug);
            if (detail is null)
            {
                continue;
            }

            summaries.Add(new ProjectSummary
            {
                Slug = detail.Slug,
                Title = detail.Title,
                Description = string.IsNullOrWhiteSpace(detail.Description) ? detail.Overview : detail.Description,
                ImageUrl = string.IsNullOrWhiteSpace(detail.ImageUrl) ? "https://via.placeholder.com/400x300?text=Proyecto" : detail.ImageUrl,
                Technology = detail.Technology,
                Rating = detail.AverageRating,
                ReviewCount = detail.ReviewCount,
                AuthorUsername = detail.AuthorUsername,
                AuthorDisplayName = string.IsNullOrWhiteSpace(detail.AuthorDisplayName) ? detail.AuthorUsername : detail.AuthorDisplayName
            });
        }

        return summaries;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
