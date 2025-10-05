using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Tarea2.Models;
using Tarea2.Services;

namespace Tarea2.Controllers;

public class AccountController : Controller
{
    private readonly IUserStoreService _userStore;
    private readonly IUserSessionService _sessionService;

    public AccountController(IUserStoreService userStore, IUserSessionService sessionService)
    {
        _userStore = userStore;
        _sessionService = sessionService;
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        return View(new SignUpViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(SignUpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_userStore.UsernameExists(model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Ese nombre de usuario ya está registrado.");
            return View(model);
        }

        var record = new UserRecord
        {
            Username = model.Username.Trim(),
            Password = model.Password,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim()
        };

        _userStore.AddUser(record);
        _sessionService.SignIn(HttpContext, record);
        TempData["SignUpSuccess"] = "Cuenta creada exitosamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Login([FromForm] string identifier, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { success = false, message = "Debes proporcionar usuario y contraseña." });
        }

        var normalizedId = identifier.Trim().ToLowerInvariant();
        var user = _userStore.GetAll().FirstOrDefault(u =>
            string.Equals(u.Username.Trim().ToLowerInvariant(), normalizedId, StringComparison.Ordinal) ||
            string.Equals(u.Email.Trim().ToLowerInvariant(), normalizedId, StringComparison.Ordinal));

        if (user is null || user.Password != password)
        {
            return BadRequest(new { success = false, message = "Credenciales inválidas." });
        }

        _sessionService.SignIn(HttpContext, user);
        return Json(new { success = true, message = $"Bienvenido {user.FirstName}" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _sessionService.SignOut(HttpContext);
        return RedirectToAction("Index", "Home");
    }
}
