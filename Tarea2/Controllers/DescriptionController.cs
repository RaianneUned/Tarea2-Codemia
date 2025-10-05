using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tarea2.Models;
using Tarea2.Services;

namespace Tarea2.Controllers;

public class DescriptionController : Controller
{
    private readonly IProjectCatalogService _catalog;
    private readonly IUserSessionService _sessionService;

    public DescriptionController(IProjectCatalogService catalog, IUserSessionService sessionService)
    {
        _catalog = catalog;
        _sessionService = sessionService;
    }

    public IActionResult Index(string slug)
    {
        var detail = _catalog.GetProjectDetail(slug);
        if (detail is null)
        {
            return RedirectToAction("Index", "Home");
        }

        detail.CanComment = _sessionService.IsAuthenticated(HttpContext);
        TrackRecentProject(detail.Slug);

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddReview(ProjectReviewInput input)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            if (isAjax)
                return Unauthorized(new { success = false, message = "Debes iniciar sesión para dejar un comentario." });
            TempData["ReviewMessage"] = "Debes iniciar sesión para dejar un comentario.";
            return RedirectToAction("Index", new { slug = input.Slug });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors?.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToArray());

            if (isAjax)
                return BadRequest(new { success = false, message = "Completa los campos requeridos.", errors });
            TempData["ReviewMessage"] = "Completa los campos requeridos.";
            return RedirectToAction("Index", new { slug = input.Slug });
        }

        var displayName = _sessionService.GetCurrentDisplayName(HttpContext) ??
                          _sessionService.GetCurrentUsername(HttpContext) ??
                          "Usuario";
        input.ReviewerName = displayName;
        input.AvatarUrl = string.Empty;

        try
        {
            var result = _catalog.AddReview(input);
            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    message = "Gracias por su comentario.",
                    averageRating = result.AverageRating,
                    reviewCount = result.ReviewCount,
                    ratingBreakdown = result.RatingBreakdown,
                    reviews = result.Reviews,
                    metrics = result.Metrics
                });
            }
            TempData["ReviewMessage"] = "Gracias por su comentario.";
            return RedirectToAction("Index", new { slug = input.Slug });
        }
        catch (Exception ex)
        {
            if (isAjax)
                return BadRequest(new { success = false, message = ex.Message });
            TempData["ReviewMessage"] = ex.Message;
            return RedirectToAction("Index", new { slug = input.Slug });
        }
    }

    private void TrackRecentProject(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        var session = HttpContext.Session;
        var stored = session.GetString(SessionConstants.RecentProjects);
        List<string> slugs;

        if (!string.IsNullOrWhiteSpace(stored))
        {
            try
            {
                slugs = JsonSerializer.Deserialize<List<string>>(stored) ?? new List<string>();
            }
            catch (JsonException)
            {
                slugs = new List<string>();
            }
        }
        else
        {
            slugs = new List<string>();
        }

        slugs.RemoveAll(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase));
        slugs.Insert(0, slug);

        const int maxItems = 8;
        if (slugs.Count > maxItems)
        {
            slugs = slugs.Take(maxItems).ToList();
        }

        session.SetString(SessionConstants.RecentProjects, JsonSerializer.Serialize(slugs));
    }
}
